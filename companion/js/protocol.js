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
  SPEAK: "SPEAK",
  PRONOUNCE: "PRONOUNCE",
});

/** ORACLE_STATE のバージョン。version 2 で手札と pairs をやめ、version 3 で mode に SCRIPT（PC で字を組む神託）を足した */
export const ORACLE_STATE_VERSION = 3;

/** PRONOUNCE のバージョン。手札の番号をやめ、名詞・動詞・行き先を持たせた（version 3） */
export const PRONOUNCE_VERSION = 3;

/** 神の言葉の最大の長さ（文字数）。SPEAK の text。ホストも同じ上限で確かめる。 */
export const MAX_GOD_TEXT_LENGTH = 80;

export const ERROR_REASON = Object.freeze({
  MAX_CONNECTIONS_REACHED: "MAX_CONNECTIONS_REACHED",
  PAIRING_FAILED: "PAIRING_FAILED",
  SESSION_REPLACED: "SESSION_REPLACED",
});

/** ORACLE_STATE の mode: 文で語りかける（VOICE）か、言葉を選んで語りかける（WORDS） */
export const ORACLE_MODE = Object.freeze({
  VOICE: "VOICE",
  WORDS: "WORDS",
  SCRIPT: "SCRIPT",
});

/** PRONOUNCE の destination、ORACLE_STATE の told.destination */
export const DESTINATION = Object.freeze({
  NONE: "NONE",
  EAST: "EAST",
  WEST: "WEST",
});

/** 方言帳の行の reason */
export const BOOK_REASON = Object.freeze({
  DIALECT: "DIALECT",
  UNKNOWN: "UNKNOWN",
  KNOWN: "KNOWN",
  MYSTERY: "MYSTERY",
});

/** バイナリフレームの種別（1バイト目）とバージョン（2バイト目）。docs/adr/0003-binary-frame-header.md */
export const FRAME_KIND = 0x01;
export const FRAME_VERSION = 1;
export const FRAME_HEADER_LENGTH = 2;

/**
 * @typedef {{ type: "PAIRED", version: 1, sessionToken: string }} PairedMessage
 * @typedef {{ type: "ERROR", version: 1, reason: string }} ErrorMessage
 * @typedef {{ requestType: string, symbolId: string, prayer: string | null }} ProphetRequest
 * @typedef {{ value: number, max: number, cost: number }} Faith
 * @typedef {{ symbolId: string, dogmaType: string }} Teaching
 * @typedef {{ year: number, teaching: Teaching | null, heardNounId: string | null, heardVerbId: string | null,
 *   destination: string, mysteryWords: string[], heardText: string | null, narration: string | null }} Told
 * @typedef {{ godWord: string, heardWordId: string | null, reason: string }} BookEntry
 * @typedef {{ type: "ORACLE_STATE", version: 3, faith: Faith, canSpeak: boolean, isDestroyed: boolean,
 *   mode: string, listening: boolean, request: ProphetRequest | null, nouns: string[], verbs: string[],
 *   told: Told | null, book: BookEntry[] }} OracleStateMessage
 * @typedef {PairedMessage | ErrorMessage | OracleStateMessage} HostMessage
 */

/** @param {string} token */
export const pair = (token) => JSON.stringify({ type: MESSAGE_TYPE.PAIR, version: MESSAGE_VERSION, token });

/** @param {string} sessionToken */
export const resume = (sessionToken) =>
  JSON.stringify({ type: MESSAGE_TYPE.RESUME, version: MESSAGE_VERSION, sessionToken });

export const startStream = () => JSON.stringify({ type: MESSAGE_TYPE.START_STREAM, version: MESSAGE_VERSION });

export const stopStream = () => JSON.stringify({ type: MESSAGE_TYPE.STOP_STREAM, version: MESSAGE_VERSION });

/** @param {string} godText 神の言葉（1〜MAX_GOD_TEXT_LENGTH 字。改行・制御文字を含まない） */
export const speak = (godText) =>
  JSON.stringify({ type: MESSAGE_TYPE.SPEAK, version: MESSAGE_VERSION, text: godText });

/**
 * @param {string | null} noun 名詞の ID
 * @param {string | null} verb 動詞の ID
 * @param {string} destination DESTINATION のどれか
 */
export const pronounce = (noun, verb, destination) =>
  JSON.stringify({ type: MESSAGE_TYPE.PRONOUNCE, version: PRONOUNCE_VERSION, noun, verb, destination });

/** @param {unknown} value */
const isText = (value) => typeof value === "string" && value !== "";

/** @param {unknown} value */
const isTextOrNull = (value) => value === null || typeof value === "string";

/** @param {any} value */
const isObject = (value) => value !== null && typeof value === "object";

/** @param {any} value */
const isTeaching = (value) => value === null || (isObject(value) && isText(value.symbolId) && isText(value.dogmaType));

/** @param {any} value */
const isTold = (value) => value === null || (isObject(value)
  && Number.isInteger(value.year) && isTeaching(value.teaching)
  && isTextOrNull(value.heardNounId) && isTextOrNull(value.heardVerbId) && isText(value.destination)
  && Array.isArray(value.mysteryWords) && value.mysteryWords.every(isText)
  && isTextOrNull(value.heardText) && isTextOrNull(value.narration));

/** @param {any} value */
const isBookEntry = (value) => isObject(value) && isText(value.godWord) && isTextOrNull(value.heardWordId) && isText(value.reason);

/** @param {any} message */
function isOracleState(message) {
  return isObject(message.faith)
    && Number.isFinite(message.faith.value) && Number.isFinite(message.faith.max) && Number.isFinite(message.faith.cost)
    && typeof message.canSpeak === "boolean"
    && typeof message.isDestroyed === "boolean"
    && Object.values(ORACLE_MODE).includes(message.mode)
    && typeof message.listening === "boolean"
    && Array.isArray(message.nouns) && message.nouns.every(isText)
    && Array.isArray(message.verbs) && message.verbs.every(isText)
    && isTold(message.told)
    && Array.isArray(message.book) && message.book.every(isBookEntry)
    && (message.request === null
      || (isObject(message.request) && isText(message.request.requestType) && isText(message.request.symbolId)
        && isTextOrNull(message.request.prayer)));
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
  if (!isObject(message)) {
    return null;
  }
  if (message.type === MESSAGE_TYPE.ORACLE_STATE) {
    return message.version === ORACLE_STATE_VERSION && isOracleState(message) ? message : null;
  }
  if (message.version !== MESSAGE_VERSION) {
    return null;
  }
  if (message.type === MESSAGE_TYPE.PAIRED && typeof message.sessionToken === "string" && message.sessionToken !== "") {
    return message;
  }
  if (message.type === MESSAGE_TYPE.ERROR && typeof message.reason === "string") {
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
