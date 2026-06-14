# Changelog

All notable changes to the Colyseus Unity SDK are documented in this file.

## 0.17.17

- Fix `Client.GetLatency()` (and therefore `Client.SelectByLatency()`) hanging on unresponsive endpoints. The measurement only settled on a pong or `OnError`, so a server that completed the WebSocket handshake then closed cleanly without replying (only `OnClose` fires) left the `Task` pending forever, and a blackholed/filtered host stalled until the OS-level TCP timeout. `GetLatency()` now also fails on `OnClose` and on a configurable timeout (`LatencyOptions.Timeout`, default `1500`ms, also forwarded through `SelectByLatency()`), so a single wedged endpoint can no longer stall the whole selection. Ports the JS SDK fix for [#941](https://github.com/colyseus/colyseus/issues/941) — thanks @TJEvans for reporting!
