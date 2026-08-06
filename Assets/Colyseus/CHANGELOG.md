# Changelog

All notable changes to the Colyseus Unity SDK are documented in this file.

## 0.17.18

- Fix server-side `client.leave(code)` being reported to the client as close code `1004`, and being treated as a dropped connection by the server. Any close code without a matching `WebSocketCloseCode` member — which is every application code, including all of Colyseus' — was collapsed into `WebSocketCloseCode.Undefined` (1004) before reaching `OnLeave`. The client also never replied to the server's close frame, so the server fell back to `1006` (abnormal closure) and ran `onDrop()` — opening a reconnection window — instead of treating a `client.leave(4000)` (`CloseCode.CONSENTED`) as a consented leave. Updates `Colyseus.NativeWebSocket` to [2.0.5](https://github.com/endel/NativeWebSocket/blob/master/CHANGELOG.md). Fixes [#948](https://github.com/colyseus/colyseus/issues/948) — thanks @trueicecold for reporting!
- Rename the WebGL plugin files from `WebSocket.jslib` / `WebSocket.jspre` to `NativeWebSocket.jslib` / `NativeWebSocket.jspre`. Unity flattens WebGL plugins into a single output directory, so the previous generic name collided with identically named plugins from other packages (Photon ships a `WebSocket.jslib`), failing the build with `Plugin 'WebSocket.jslib' is used from several locations`. When upgrading via `.unitypackage`, delete the old `Assets/Colyseus/Runtime/WebSocket/WebSocket.jslib` and `WebSocket.jspre` — importing does not remove files, and the old and new plugins collide with each other. Installing via UPM is not affected. Thanks @Alaadel for reporting!

## 0.17.17

- Fix `Client.GetLatency()` (and therefore `Client.SelectByLatency()`) hanging on unresponsive endpoints. The measurement only settled on a pong or `OnError`, so a server that completed the WebSocket handshake then closed cleanly without replying (only `OnClose` fires) left the `Task` pending forever, and a blackholed/filtered host stalled until the OS-level TCP timeout. `GetLatency()` now also fails on `OnClose` and on a configurable timeout (`LatencyOptions.Timeout`, default `1500`ms, also forwarded through `SelectByLatency()`), so a single wedged endpoint can no longer stall the whole selection. Ports the JS SDK fix for [#941](https://github.com/colyseus/colyseus/issues/941) — thanks @TJEvans for reporting!
