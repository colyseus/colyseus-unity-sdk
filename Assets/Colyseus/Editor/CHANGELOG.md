# Colyseus Room Inspector - Changelog

## [1.0.0] - 2025-11-28

### Added
- Initial release of Colyseus Room Inspector
- Unity Editor window for real-time room state inspection
- Automatic room discovery via reflection
- Support for both instance and static ColyseusRoom fields
- Real-time state inspection with auto-refresh (0.5s interval)
- Hierarchical display of Schema objects
- Support for MapSchema<T> and ArraySchema<T> collections
- Collapsible foldout UI for organized viewing
- Connection status monitoring
- Copy state to clipboard functionality
- Comprehensive documentation
  - User guide (RoomInspector-README.md)
  - Quick start guide (QUICKSTART.md)
  - Implementation notes (IMPLEMENTATION_NOTES.md)

### Features
- **Auto-refresh**: Toggle automatic updates every 0.5 seconds
- **Manual refresh**: Force immediate update
- **Multi-room support**: Display multiple connected rooms simultaneously
- **Deep inspection**: Recursive traversal up to 10 levels
- **Collection limits**: Display up to 100 items in collections
- **Read-only fields**: Selectable text for easy copying
- **Type-aware rendering**: Different display for primitives vs objects
- **Menu integration**: Accessible via Window → Colyseus → Room Inspector

### Technical Details
- Assembly definition: Colyseus.Editor.asmdef
- Namespace: ColyseusEditor
- Dependencies: ColyseusSDK assembly
- Platform: Editor only
- Uses reflection for type-agnostic inspection
- Compatible with all Unity versions supporting the Colyseus SDK

### Documentation
- Complete user documentation
- Implementation technical notes
- Quick start guide
- Troubleshooting guide
- Example usage patterns

### Performance
- Throttled refresh rate (0.5s)
- Collection display limited to 100 items
- Recursion depth limited to 10 levels
- Duplicate room detection
- Lazy evaluation of collapsed sections

### Known Limitations
- Play mode only (not available in Edit mode)
- Read-only display (no state editing)
- Text format export (not true JSON)
- Large collections truncated
- Deep nesting truncated

### Future Roadmap
- JSON export format
- State diff/comparison view
- Message inspection
- Performance metrics
- State history tracking
- Search and filter
- Export to file
- Context menu integration

---

## How to Use

1. Open Unity Editor
2. Navigate to Window → Colyseus → Room Inspector
3. Enter Play mode
4. Connect to a Colyseus server
5. View and inspect your room state in real-time

For detailed instructions, see [QUICKSTART.md](QUICKSTART.md)

---

## Credits

Developed as an enhancement to the Colyseus Unity SDK.
Built with Unity's EditorWindow API and C# Reflection.

---

## License

MIT License - Same as Colyseus Unity SDK

