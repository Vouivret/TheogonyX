// コンパニオンの画面。ペアリング・再接続・画面の配信の受け取り（docs/domain/companion-pairing.md・frame-stream.md）。
// トークンの値はログに出さない。

import * as protocol from "./protocol.js";

const SESSION_TOKEN_KEY = "theogonyx.sessionToken";
const RECONNECT_DELAY_MS = 2000;
const SUPPORTED_LANGUAGES = ["ja", "en"];

const canvas = /** @type {HTMLCanvasElement} */ (document.getElementById("world-canvas"));
const context = /** @type {CanvasRenderingContext2D} */ (canvas.getContext("2d"));
const statusLine = /** @type {HTMLElement} */ (document.getElementById("status"));

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

  // ERROR の後はホストが接続を閉じる。自動では再接続しない。
  reconnectAllowed = false;
  if (message.reason === protocol.ERROR_REASON.PAIRING_FAILED) {
    sessionStorage.removeItem(SESSION_TOKEN_KEY);
    showStatus("ERROR_PAIRING_FAILED");
  } else if (message.reason === protocol.ERROR_REASON.MAX_CONNECTIONS_REACHED) {
    showStatus("ERROR_MAX_CONNECTIONS_REACHED");
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
connect();
