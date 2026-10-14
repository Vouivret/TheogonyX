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
  ORACLE_STATE: "ORACLE_STATE",
  PRONOUNCE: "PRONOUNCE",
});

/** PRONOUNCE のバージョン。TAMPER を廃止し、手札の番号 2 つを持たせた（version 2） */
export const PRONOUNCE_VERSION = 2;

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
 * @typedef {{ requestType: string, symbolId: string }} ProphetRequest
 * @typedef {{ value: number, max: number, cost: number }} Faith
 * @typedef {{ type: "ORACLE_STATE", version: 1, hand: string[], faith: Faith,
 *   canPronounce: boolean, isDestroyed: boolean, request: ProphetRequest | null,
 *   pairs?: { first: number, second: number, relation: string, dogmaType: string }[] }} OracleStateMessage
 * @typedef {PairedMessage | ErrorMessage | OracleStateMessage} HostMessage
 */

/** @param {string} token */
export const pair = (token) => JSON.stringify({ type: MESSAGE_TYPE.PAIR, version: MESSAGE_VERSION, token });

/** @param {string} sessionToken */
export const resume = (sessionToken) =>
  JSON.stringify({ type: MESSAGE_TYPE.RESUME, version: MESSAGE_VERSION, sessionToken });

export const startStream = () => JSON.stringify({ type: MESSAGE_TYPE.START_STREAM, version: MESSAGE_VERSION });

export const stopStream = () => JSON.stringify({ type: MESSAGE_TYPE.STOP_STREAM, version: MESSAGE_VERSION });

/**
 * @param {number} first 1 枚目（ドグマの対象）の手札の番号（0 から）
 * @param {number} second 2 枚目の手札の番号（1 枚目と違う番号）
 */
export const pronounce = (first, second) =>
  JSON.stringify({ type: MESSAGE_TYPE.PRONOUNCE, version: PRONOUNCE_VERSION, first, second });

/** @param {unknown} value */
const isText = (value) => typeof value === "string" && value !== "";

/** @param {any} value */
const isPair = (value) => value !== null && typeof value === "object"
  && Number.isInteger(value.first) && Number.isInteger(value.second) && isText(value.relation) && isText(value.dogmaType);

/** @param {any} message */
function isOracleState(message) {
  return Array.isArray(message.hand)
    && message.hand.every(isText)
    && message.faith !== null && typeof message.faith === "object"
    && Number.isFinite(message.faith.value) && Number.isFinite(message.faith.max) && Number.isFinite(message.faith.cost)
    && typeof message.canPronounce === "boolean"
    && typeof message.isDestroyed === "boolean"
    && (message.pairs === undefined || (Array.isArray(message.pairs) && message.pairs.every(isPair)))
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
  if (message.type === MESSAGE_TYPE.ORACLE_STATE && isOracleState(message)) {
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
