﻿namespace ErogeHelper.Shared.Contracts;

public static class CommonGlyphs
{
    // See https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font#icon-list

    public const string BackToWindow = "\uE1D8";

    public const string FullScreen = "\uE1D9";

    public const string Brightness = "\uE706";

    public const string Search = "\uE721";

    public const string Share = "\uE72D";

    public const string Cloud = "\uE753";

    public const string KeyboardClassic = "\uE765";

    public const string Unpin = "\uE77A";

    public const string Color = "\uE790";

    public const string TaskView = "\uE7C4";

    public const string LangJPN = "\uE7DE";

    public const string Sort = "\uE8CB";

    public const string Info = "\uE946";

    public const string Component = "\uE950";

    public const string Volume1 = "\uE993";

    public const string FastForward = "\uEB9D";

    public const string LowerBrightness = "\uEC8A";

    public const string ClipboardList = "\uF0E3";

    public const string GroupList = "\uF168";
}

public enum SymbolName
{
    BackToWindow,
    FullScreen,
}

public enum SymbolExtend
{
    Tablet = 0xE70A,
    TaskView = 0xE7C4,
    Volume1 = 0xE993,
    SettingSolid = 0xF8B0,
    TBD = 0xFFFF
}
