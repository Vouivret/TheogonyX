// コンパニオンの画面。ペアリング・再接続・画面の配信の受け取り（docs/domain/companion-pairing.md・frame-stream.md）と、
// 夢の改ざん・神託・依頼の吹き出し（dream-tampering.md・prophet-request.md）。
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
const dreamRow = /** @type {HTMLElement} */ (document.getElementById("dream-row"));
const dreamNodes = /** @type {HTMLElement} */ (document.getElementById("dream-nodes"));
const oracleButton = /** @type {HTMLButtonElement} */ (document.getElementById("oracle-button"));
const dreamCards = /** @type {HTMLElement} */ (document.getElementById("dream-cards"));
const cardButtons = /** @type {HTMLElement} */ (document.getElementById("card-buttons"));
const cancelButton = /** @type {HTMLButtonElement} */ (document.getElementById("cancel-button"));

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
/** @type {import("./protocol.js").DreamStateMessage | null} ホストから最後に受け取った夢の状態 */
let dreamState = null;
/** 改ざんするために選んだ夢ノードの位置。選んでいなければ -1。 */
let selectedIndex = -1;

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
    renderDream();
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

  if (message.type === protocol.MESSAGE_TYPE.DREAM_STATE) {
    dreamState = message;
    renderDream();
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
 * ボタンの数を count に合わせる。押してから離すまでに描き直しても押せるよう、ボタンは使い回す。
 * @param {HTMLElement} container
 * @param {number} count
 * @param {(index: number) => void} onPress
 * @returns {HTMLButtonElement[]}
 */
function syncButtons(container, count, onPress) {
  while (container.children.length < count) {
    const index = container.children.length;
    const button = document.createElement("button");
    button.type = "button";
    button.addEventListener("click", () => onPress(index));
    container.append(button);
  }
  while (container.children.length > count) {
    container.lastElementChild?.remove();
  }
  return /** @type {HTMLButtonElement[]} */ ([...container.children]);
}

/**
 * @param {string} key
 * @param {...string} values {0}・{1} … に入れる値
 */
const format = (key, ...values) => text(key).replace(/\{(\d+)\}/g, (match, index) => values[Number(index)] ?? match);

/** @param {string} symbolId */
const symbolName = (symbolId) => text(`SYMBOL_${symbolId}`);

// 夢の欄と依頼の吹き出しを、最後に受け取った状態で描き直す。切断しているあいだは押せなくする。
function renderDream() {
  const state = isPaired ? dreamState : null;
  const canTamper = state?.canTamper ?? false;
  if (!canTamper || selectedIndex >= (state?.nodes.length ?? 0)) {
    selectedIndex = -1;
  }
  const isChoosing = selectedIndex >= 0;

  const nodes = state?.nodes ?? dreamState?.nodes ?? [];
  syncButtons(dreamNodes, nodes.length, onNodePressed).forEach((button, index) => {
    const name = document.createElement("span");
    name.textContent = symbolName(nodes[index].symbolId);
    const weight = document.createElement("span");
    weight.className = "dream-node-weight";
    weight.textContent = format("DREAM_NODE_WEIGHT", nodes[index].weight.toFixed(2));
    button.replaceChildren(name, weight);
    button.disabled = !canTamper;
  });
  oracleButton.disabled = !(state?.canPronounce ?? false);

  const cards = state?.cards ?? [];
  syncButtons(cardButtons, cards.length, (index) => onCardChosen(dreamState?.cards[index])).forEach((button, index) => {
    button.textContent = symbolName(cards[index]);
  });
  dreamRow.hidden = isChoosing;
  dreamCards.hidden = !isChoosing;
  dreamGuide.textContent = text(guideKey(state, isChoosing));

  const request = state?.request ?? null;
  requestBubble.hidden = request === null;
  if (request !== null) {
    requestBubble.textContent = format("REQUEST_BUBBLE", text(`TROUBLE_${request.requestType}`), symbolName(request.symbolId));
  }
}

/**
 * @param {import("./protocol.js").DreamStateMessage | null} state
 * @param {boolean} isChoosing
 */
function guideKey(state, isChoosing) {
  if (state === null) {
    return "DREAM_GUIDE_OFFLINE";
  }
  if (state.isDestroyed) {
    return "DREAM_GUIDE_DESTROYED";
  }
  if (isChoosing) {
    return "DREAM_GUIDE_CHOOSE_CARD";
  }
  switch (state.state) {
    case protocol.DREAM_STATE.UNTAMPERED:
      return "DREAM_GUIDE_UNTAMPERED";
    case protocol.DREAM_STATE.TAMPERED:
      return "DREAM_GUIDE_TAMPERED";
    default:
      return "DREAM_NONE";
  }
}

// 同じコマをもう一度押すか「やめる」で、選ぶのをやめる
/** @param {number} index */
function onNodePressed(index) {
  selectedIndex = selectedIndex === index ? -1 : index;
  renderDream();
}

// 送った後はホストの DREAM_STATE で描き直す。PC が先に改ざんしていたら、ホストが今の状態を送り直す
/** @param {string | undefined} symbolId */
function onCardChosen(symbolId) {
  if (selectedIndex >= 0 && isPaired && symbolId !== undefined) {
    socket?.send(protocol.tamper(selectedIndex, symbolId));
  }
  selectedIndex = -1;
  renderDream();
}

oracleButton.addEventListener("click", () => {
  if (isPaired) {
    socket?.send(protocol.pronounce());
  }
});
cancelButton.addEventListener("click", () => onNodePressed(selectedIndex));

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
renderDream();
connect();
