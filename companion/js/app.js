// コンパニオンの画面。ペアリング・再接続・画面の配信の受け取り（docs/domain/companion-pairing.md・frame-stream.md）と、
// 神の言葉を文で書く（または言葉を選ぶ）神託・告げた神託・方言帳・神威・依頼の吹き出し（oracle-pair.md・prophet-request.md）。
// トークンの値はログに出さない。

import * as protocol from "./protocol.js";

const SESSION_TOKEN_KEY = "theogonyx.sessionToken";
const RECONNECT_DELAY_MS = 2000;
const SUPPORTED_LANGUAGES = ["ja", "en"];

const canvas = /** @type {HTMLCanvasElement} */ (document.getElementById("world-canvas"));
const context = /** @type {CanvasRenderingContext2D} */ (canvas.getContext("2d"));
const statusLine = /** @type {HTMLElement} */ (document.getElementById("status"));
const requestBubble = /** @type {HTMLElement} */ (document.getElementById("request-bubble"));
const dreamGuide = /** @type {HTMLElement} */ (document.getElementById("dream-guide"));
const voiceRow = /** @type {HTMLElement} */ (document.getElementById("voice-row"));
const wordsRow = /** @type {HTMLElement} */ (document.getElementById("words-row"));
const godText = /** @type {HTMLInputElement} */ (document.getElementById("god-text"));
const speakButton = /** @type {HTMLButtonElement} */ (document.getElementById("speak-button"));
const nounSelect = /** @type {HTMLSelectElement} */ (document.getElementById("noun-select"));
const verbSelect = /** @type {HTMLSelectElement} */ (document.getElementById("verb-select"));
const destinationSelect = /** @type {HTMLSelectElement} */ (document.getElementById("destination-select"));
const chooseButton = /** @type {HTMLButtonElement} */ (document.getElementById("choose-button"));
const toldBox = /** @type {HTMLElement} */ (document.getElementById("told"));
const toldLine = /** @type {HTMLElement} */ (document.getElementById("told-line"));
const toldHeard = /** @type {HTMLElement} */ (document.getElementById("told-heard"));
const toldNarration = /** @type {HTMLElement} */ (document.getElementById("told-narration"));
const toldDestination = /** @type {HTMLElement} */ (document.getElementById("told-destination"));
const toldMystery = /** @type {HTMLElement} */ (document.getElementById("told-mystery"));
const bookList = /** @type {HTMLElement} */ (document.getElementById("book-list"));

/** @type {Record<string, string>} */
let texts = {};
/** @type {WebSocket | null} */
let socket = null;
/** @type {string | null} URL から読んだワンタイムトークン。1回送ったら捨てる。 */
let oneTimeToken = null;
let isPaired = false;
let reconnectAllowed = true;
/** @type {ArrayBuffer | null} 描くのを待っている最新のコマ */
let pendingFrame = null;
let isDrawing = false;
/** @type {import("./protocol.js").OracleStateMessage | null} ホストから最後に受け取った神託の状態 */
let oracleState = null;
/** 描いている方言帳（変わっていなければ描き直さない）。 */
let bookShown = "";

context.imageSmoothingEnabled = false;

/** @param {string} key */
const text = (key) => texts[key] ?? key;

/** @param {string} key */
function showStatus(key) {
  statusLine.textContent = text(key);
}

async function loadTexts() {
  const language = SUPPORTED_LANGUAGES.find((code) => navigator.language.startsWith(code)) ?? SUPPORTED_LANGUAGES[0];
  const response = await fetch(`locales/${language}.json`);
  if (!response.ok) {
    throw new Error(`event=locale_load_failed reason=http_${response.status}`);
  }
  texts = await response.json();
  document.documentElement.lang = language;
  for (const element of document.querySelectorAll("[data-i18n]")) {
    element.textContent = text(/** @type {HTMLElement} */ (element).dataset.i18n ?? "");
  }
  for (const element of document.querySelectorAll("[data-i18n-placeholder]")) {
    /** @type {HTMLInputElement} */ (element).placeholder = text(/** @type {HTMLElement} */ (element).dataset.i18nPlaceholder ?? "");
  }
}

// URL のトークンは読んだらすぐに消す（履歴・共有・再読み込みで残さない）
function takeTokenFromUrl() {
  const token = new URLSearchParams(location.search).get("token");
  if (token !== null) {
    history.replaceState(null, "", location.pathname);
  }
  return token;
}

function connect() {
  const sessionToken = sessionStorage.getItem(SESSION_TOKEN_KEY);
  if (oneTimeToken === null && sessionToken === null) {
    showStatus("STATUS_SCAN_QR");
    return;
  }

  showStatus("STATUS_CONNECTING");
  socket = new WebSocket(`ws://${location.hostname}:${protocol.WEBSOCKET_PORT}/`);
  socket.binaryType = "arraybuffer";

  socket.addEventListener("open", () => {
    if (oneTimeToken !== null) {
      socket?.send(protocol.pair(oneTimeToken));
      oneTimeToken = null;
    } else if (sessionToken !== null) {
      socket?.send(protocol.resume(sessionToken));
    }
  });

  socket.addEventListener("message", (event) => {
    if (typeof event.data === "string") {
      handleText(event.data);
    } else {
      receiveFrame(event.data);
    }
  });

  socket.addEventListener("close", () => {
    socket = null;
    isPaired = false;
    renderOracle();
    if (!reconnectAllowed) {
      return; // ERROR の文言を残す
    }
    if (sessionStorage.getItem(SESSION_TOKEN_KEY) !== null) {
      showStatus("STATUS_RECONNECTING");
      setTimeout(connect, RECONNECT_DELAY_MS);
    } else {
      // ペアリングが済む前に切れた。ワンタイムトークンは使ってしまったので、読み直してもらう
      showStatus("STATUS_SCAN_QR");
    }
  });
}

/** @param {string} data */
function handleText(data) {
  const message = protocol.parseHostMessage(data);
  if (message === null) {
    console.warn("event=message_rejected reason=unknown_type_or_version");
    return;
  }

  if (message.type === protocol.MESSAGE_TYPE.PAIRED) {
    sessionStorage.setItem(SESSION_TOKEN_KEY, message.sessionToken);
    isPaired = true;
    showStatus("STATUS_PAIRED");
    if (document.visibilityState === "visible") {
      socket?.send(protocol.startStream());
    }
    return;
  }

  if (message.type === protocol.MESSAGE_TYPE.ORACLE_STATE) {
    oracleState = message;
    renderOracle();
    return;
  }

  // ERROR の後はホストが接続を閉じる。自動では再接続しない。
  reconnectAllowed = false;
  if (message.reason === protocol.ERROR_REASON.PAIRING_FAILED) {
    sessionStorage.removeItem(SESSION_TOKEN_KEY);
    showStatus("ERROR_PAIRING_FAILED");
  } else if (message.reason === protocol.ERROR_REASON.MAX_CONNECTIONS_REACHED) {
    showStatus("ERROR_MAX_CONNECTIONS_REACHED");
  } else if (message.reason === protocol.ERROR_REASON.SESSION_REPLACED) {
    // 同じ sessionToken で別の画面がつないだ。取り合いにならないよう、再接続しない
    showStatus("ERROR_SESSION_REPLACED");
  } else {
    console.warn("event=message_rejected reason=unknown_error_reason");
  }
}

/** @param {ArrayBuffer} buffer */
function receiveFrame(buffer) {
  // 描いている間に来たコマは、最新の1つだけを残す
  pendingFrame = buffer;
  if (!isDrawing) {
    void drawFrames();
  }
}

async function drawFrames() {
  isDrawing = true;
  while (pendingFrame !== null) {
    const buffer = pendingFrame;
    pendingFrame = null;
    const jpeg = protocol.frameJpeg(buffer);
    if (jpeg === null) {
      console.warn("event=message_rejected reason=unknown_frame_kind_or_version");
      continue;
    }
    try {
      const bitmap = await createImageBitmap(new Blob([jpeg], { type: "image/jpeg" }));
      context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
      bitmap.close();
    } catch (error) {
      console.warn(`event=frame_decode_failed error=${error instanceof Error ? error.name : "unknown"}`);
    }
  }
  isDrawing = false;
}

/**
 * @param {string} key
 * @param {...string} values {0}・{1} … に入れる値
 */
const format = (key, ...values) => text(key).replace(/\{(\d+)\}/g, (match, index) => values[Number(index)] ?? match);

/** @param {string} symbolId */
const symbolName = (symbolId) => text(`SYMBOL_${symbolId}`);

/** @param {string} wordId */
const wordName = (wordId) => text(`WORD_${wordId}`);

/** 神の言葉として送れるか。1〜MAX_GOD_TEXT_LENGTH 字で、制御文字を含まない（ホストも同じ検証をする）。 */
const isValidGodText = (/** @type {string} */ value) =>
  value.trim() !== "" && value.length <= protocol.MAX_GOD_TEXT_LENGTH && !/\p{Cc}/u.test(value);

/**
 * 選ぶ欄の項目を ids に合わせる。同じ項目なら作り直さない（選んでいる途中で消さないため）。先頭は「なし」。
 * @param {HTMLSelectElement} select
 * @param {readonly string[]} ids
 * @param {(id: string) => string} label
 */
function syncSelect(select, ids, label) {
  const signature = ids.join(",");
  if (select.dataset.signature === signature) {
    return;
  }
  select.dataset.signature = signature;
  select.replaceChildren(new Option(text("CHOICE_NONE"), ""), ...ids.map((id) => new Option(label(id), id)));
}

// 神託の欄・告げた神託・方言帳・依頼の吹き出しを、最後に受け取った状態で描き直す。切断しているあいだは押せなくする。
function renderOracle() {
  const state = isPaired ? oracleState : null;
  const shown = state ?? oracleState;
  const wordsMode = shown?.mode === protocol.ORACLE_MODE.WORDS;
  // 字の神託は PC の画面で字を組むので、スマホでは入力欄も選ぶ欄も出さない
  const scriptMode = shown?.mode === protocol.ORACLE_MODE.SCRIPT;
  const canSpeak = state?.canSpeak ?? false;

  voiceRow.hidden = wordsMode || scriptMode;
  wordsRow.hidden = !wordsMode || scriptMode;
  godText.disabled = !canSpeak;
  speakButton.disabled = !(canSpeak && isValidGodText(godText.value));
  if (wordsMode && shown !== null) {
    syncSelect(nounSelect, shown.nouns, wordName);
    syncSelect(verbSelect, shown.verbs, wordName);
    if (destinationSelect.options.length === 0) {
      destinationSelect.replaceChildren(
        new Option(text("DIRECTION_NONE"), protocol.DESTINATION.NONE),
        new Option(text("DIRECTION_EAST"), protocol.DESTINATION.EAST),
        new Option(text("DIRECTION_WEST"), protocol.DESTINATION.WEST));
    }
  }
  for (const select of [nounSelect, verbSelect, destinationSelect]) {
    select.disabled = !canSpeak;
  }
  const chosen = nounSelect.value !== "" || verbSelect.value !== "" || destinationSelect.value !== protocol.DESTINATION.NONE;
  chooseButton.disabled = !(canSpeak && chosen);
  dreamGuide.textContent = guideText(state);

  const request = state?.request ?? null;
  requestBubble.hidden = request === null;
  if (request !== null) {
    // ホストが祈りの文を作っていれば、その文。まだなければ、依頼の定型の文
    requestBubble.textContent = request.prayer
      ?? format("REQUEST_BUBBLE", text(`TROUBLE_${request.requestType}`), symbolName(request.symbolId));
  }
  renderTold(shown?.told ?? null);
  renderBook(shown?.book ?? []);
}

/**
 * 今の状態の案内と、神威（今の量 / 上限）。
 * @param {import("./protocol.js").OracleStateMessage | null} state
 */
function guideText(state) {
  if (state === null) {
    return text("ORACLE_GUIDE_OFFLINE");
  }
  if (state.isDestroyed) {
    return text("ORACLE_GUIDE_DESTROYED");
  }
  let key = "ORACLE_GUIDE_SPEAK";
  if (state.listening) {
    key = "ORACLE_GUIDE_LISTENING";
  } else if (state.faith.value + 1e-6 < state.faith.cost) {
    key = "ORACLE_GUIDE_NO_FAITH";
  } else if (state.mode === protocol.ORACLE_MODE.SCRIPT) {
    key = "ORACLE_GUIDE_SCRIPT";
  } else if (state.mode === protocol.ORACLE_MODE.WORDS) {
    key = "ORACLE_GUIDE_CHOOSE_WORDS";
  }
  return format("ORACLE_GUIDE_FAITH", text(key), state.faith.value.toFixed(1), state.faith.max.toFixed(1));
}

/** @param {HTMLElement} element @param {string} value 空なら隠す */
function setLine(element, value) {
  element.textContent = value;
  element.hidden = value === "";
}

// 告げた神託: 掟の 1 行、聞こえた言葉（LLM の聞こえた文があればその文）、語り、行き先、謎の言葉。文は textContent で入れる
/** @param {import("./protocol.js").Told | null} told */
function renderTold(told) {
  toldBox.hidden = told === null;
  if (told === null) {
    return;
  }
  setLine(toldLine, told.teaching === null ? "" : format(`ORACLE_LINE_${told.teaching.dogmaType}`, symbolName(told.teaching.symbolId)));
  const heardRule = told.heardNounId === null && told.heardVerbId === null
    ? ""
    : format("ORACLE_HEARD_RULE", told.heardNounId === null ? "-" : wordName(told.heardNounId), told.heardVerbId === null ? "-" : wordName(told.heardVerbId));
  setLine(toldHeard, told.heardText ?? heardRule);
  setLine(toldNarration, told.narration ?? "");
  setLine(
    toldDestination,
    told.destination === protocol.DESTINATION.EAST ? text("ORACLE_DESTINATION_EAST")
      : told.destination === protocol.DESTINATION.WEST ? text("ORACLE_DESTINATION_WEST") : "");
  setLine(toldMystery, told.mysteryWords.length === 0 ? "" : format("ORACLE_MYSTERY", told.mysteryWords.join("、")));
}

// 方言帳: 「神の言葉 → 予言者に聞こえた言葉（理由）」を新しい順に出す
/** @param {readonly import("./protocol.js").BookEntry[]} book */
function renderBook(book) {
  const signature = JSON.stringify(book);
  if (signature === bookShown) {
    return;
  }
  bookShown = signature;
  const items = [...book].reverse().map((entry) => {
    const item = document.createElement("li");
    item.textContent = bookLine(entry);
    return item;
  });
  if (items.length === 0) {
    const empty = document.createElement("li");
    empty.textContent = text("BOOK_EMPTY");
    items.push(empty);
  }
  bookList.replaceChildren(...items);
}

/** @param {import("./protocol.js").BookEntry} entry */
function bookLine(entry) {
  const heard = entry.heardWordId === null ? "" : wordName(entry.heardWordId);
  switch (entry.reason) {
    case protocol.BOOK_REASON.MYSTERY:
      return format("BOOK_ROW_MYSTERY", entry.godWord);
    case protocol.BOOK_REASON.KNOWN:
      return format("BOOK_ROW_KNOWN", wordName(entry.godWord));
    case protocol.BOOK_REASON.UNKNOWN:
      return format("BOOK_ROW_UNKNOWN", wordName(entry.godWord), heard);
    default:
      return format("BOOK_ROW_DIALECT", wordName(entry.godWord), heard);
  }
}

// 送った後はホストの ORACLE_STATE で描き直す。PC が先に語りかけていたら、ホストが今の状態を送り直す
function submitSpeak() {
  const value = godText.value.trim();
  if (isPaired && oracleState?.canSpeak && isValidGodText(value)) {
    socket?.send(protocol.speak(value));
    godText.value = "";
    renderOracle();
  }
}

function submitChoice() {
  if (isPaired && oracleState?.canSpeak) {
    socket?.send(protocol.pronounce(nounSelect.value || null, verbSelect.value || null, destinationSelect.value || protocol.DESTINATION.NONE));
    for (const select of [nounSelect, verbSelect, destinationSelect]) {
      select.selectedIndex = 0;
    }
    renderOracle();
  }
}

speakButton.addEventListener("click", submitSpeak);
chooseButton.addEventListener("click", submitChoice);
godText.addEventListener("input", renderOracle);
godText.addEventListener("keydown", (event) => {
  if (event.key === "Enter" && !event.isComposing) {
    submitSpeak();
  }
});
for (const select of [nounSelect, verbSelect, destinationSelect]) {
  select.addEventListener("change", renderOracle);
}

document.addEventListener("visibilitychange", () => {
  if (!isPaired || socket === null) {
    return;
  }
  socket.send(document.visibilityState === "visible" ? protocol.startStream() : protocol.stopStream());
});

oneTimeToken = takeTokenFromUrl();
try {
  await loadTexts();
} catch (error) {
  console.warn(`event=locale_load_failed error=${error instanceof Error ? error.message : "unknown"}`);
}
renderOracle();
connect();
