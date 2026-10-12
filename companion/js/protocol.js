// ホストとのメッセージの定義。docs/protocol/messages.md の表と1対1で対応させる。
// 表を変えたら、同じ変更でここも直す（.agent/rules/protocol.md）。

/** WebSocket のポート。game/data/companion.json の webSocketPort と同じにする。 */
export const WEBSOCKET_PORT = 8081;

export const MESSAGE_VERSION = 1;

export const MESSAGE_TYPE = Object.freeze({
  PAIR: "PAIR",
  RESUME: "RESUME",
  PAIRED: "PAIRED",
  ERROR: "ERROR",
  START_STREAM: "START_STREAM",
  STOP_STREAM: "STOP_STREAM",
  DREAM_STATE: "DREAM_STATE",
  TAMPER: "TAMPER",
  PRONOUNCE: "PRONOUNCE",
});

/** DREAM_STATE の state（予言者の夢の状態） */
export const DREAM_STATE = Object.freeze({
  NONE: "NONE",
  UNTAMPERED: "UNTAMPERED",
  TAMPERED: "TAMPERED",
});

export const ERROR_REASON = Object.freeze({
  MAX_CONNECTIONS_REACHED: "MAX_CONNECTIONS_REACHED",
  PAIRING_FAILED: "PAIRING_FAILED",
  SESSION_REPLACED: "SESSION_REPLACED",
});

/** バイナリフレームの種別（1バイト目）とバージョン（2バイト目）。docs/adr/0003-binary-frame-header.md */
export const FRAME_KIND = 0x01;
export const FRAME_VERSION = 1;
export const FRAME_HEADER_LENGTH = 2;

/**
 * @typedef {{ type: "PAIRED", version: 1, sessionToken: string }} PairedMessage
 * @typedef {{ type: "ERROR", version: 1, reason: string }} ErrorMessage
 * @typedef {{ symbolId: string, weight: number }} DreamNode
 * @typedef {{ requestType: string, symbolId: string }} ProphetRequest
 * @typedef {{ type: "DREAM_STATE", version: 1, state: string, nodes: DreamNode[], cards: string[],
 *   canTamper: boolean, canPronounce: boolean, isDestroyed: boolean, request: ProphetRequest | null }} DreamStateMessage
 * @typedef {PairedMessage | ErrorMessage | DreamStateMessage} HostMessage
 */

/** @param {string} token */
export const pair = (token) => JSON.stringify({ type: MESSAGE_TYPE.PAIR, version: MESSAGE_VERSION, token });

/** @param {string} sessionToken */
export const resume = (sessionToken) =>
  JSON.stringify({ type: MESSAGE_TYPE.RESUME, version: MESSAGE_VERSION, sessionToken });

export const startStream = () => JSON.stringify({ type: MESSAGE_TYPE.START_STREAM, version: MESSAGE_VERSION });

export const stopStream = () => JSON.stringify({ type: MESSAGE_TYPE.STOP_STREAM, version: MESSAGE_VERSION });

/**
 * @param {number} index 夢の何番目か（0 から）
 * @param {string} symbolId 入れ替えるカードの記号 ID
 */
export const tamper = (index, symbolId) =>
  JSON.stringify({ type: MESSAGE_TYPE.TAMPER, version: MESSAGE_VERSION, index, symbolId });

export const pronounce = () => JSON.stringify({ type: MESSAGE_TYPE.PRONOUNCE, version: MESSAGE_VERSION });

/** @param {unknown} value */
const isText = (value) => typeof value === "string" && value !== "";

/** @param {any} message */
function isDreamState(message) {
  return Object.values(DREAM_STATE).includes(message.state)
    && Array.isArray(message.nodes)
    && message.nodes.every((/** @type {any} */ node) =>
      node !== null && typeof node === "object" && isText(node.symbolId) && Number.isFinite(node.weight))
    && Array.isArray(message.cards)
    && message.cards.every(isText)
    && typeof message.canTamper === "boolean"
    && typeof message.canPronounce === "boolean"
    && typeof message.isDestroyed === "boolean"
    && (message.request === null
      || (typeof message.request === "object" && isText(message.request.requestType) && isText(message.request.symbolId)));
}

/**
 * ホストからのテキストフレームを検証する。知らないもの・形の違うものは null。
 * @param {string} text
 * @returns {HostMessage | null}
 */
export function parseHostMessage(text) {
  let message;
  try {
    message = JSON.parse(text);
  } catch {
    return null;
  }
  if (message === null || typeof message !== "object" || message.version !== MESSAGE_VERSION) {
    return null;
  }
  if (message.type === MESSAGE_TYPE.PAIRED && typeof message.sessionToken === "string" && message.sessionToken !== "") {
    return message;
  }
  if (message.type === MESSAGE_TYPE.ERROR && typeof message.reason === "string") {
    return message;
  }
  if (message.type === MESSAGE_TYPE.DREAM_STATE && isDreamState(message)) {
    return message;
  }
  return null;
}

/**
 * FRAME のバイナリフレームから JPEG の部分を返す。知らない種別・バージョンなら null。
 * @param {ArrayBuffer} buffer
 * @returns {Uint8Array | null}
 */
export function frameJpeg(buffer) {
  const bytes = new Uint8Array(buffer);
  if (bytes.length <= FRAME_HEADER_LENGTH || bytes[0] !== FRAME_KIND || bytes[1] !== FRAME_VERSION) {
    return null;
  }
  return bytes.subarray(FRAME_HEADER_LENGTH);
}
