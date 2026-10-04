namespace OmniEurope.Blazor.Components;

/// <summary>
/// A built-in icon, drawn by <see cref="OmniIcon"/> from the Phosphor Icons "regular" outlines the
/// package ships, one outline per value. <see cref="OmniIcon.Glyph"/> draws any other outline.
/// </summary>
public enum OmniIconName
{
    /// <summary>Check.</summary>
    Check,
    /// <summary>Cross, used to close or dismiss.</summary>
    Close,
    /// <summary>Info.</summary>
    Info,
    /// <summary>Warning.</summary>
    Warning,
    /// <summary>Chevron down.</summary>
    ChevronDown,
    /// <summary>Menu (three horizontal lines). Also the outline drawn for a value outside this enumeration.</summary>
    Menu,
    /// <summary>Filter.</summary>
    Filter,
    /// <summary>Sort ascending.</summary>
    SortAscending,
    /// <summary>Sort descending.</summary>
    SortDescending,
    /// <summary>Dark appearance.</summary>
    ThemeDark,
    /// <summary>Light appearance.</summary>
    ThemeLight,
    /// <summary>Plus sign, used to add.</summary>
    Add,
    /// <summary>Airplane.</summary>
    Airplane,
    /// <summary>Airplane landing.</summary>
    AirplaneLanding,
    /// <summary>Airplane takeoff.</summary>
    AirplaneTakeoff,
    /// <summary>Archive.</summary>
    Archive,
    /// <summary>Arrow left.</summary>
    ArrowLeft,
    /// <summary>Arrow right.</summary>
    ArrowRight,
    /// <summary>Bed.</summary>
    Bed,
    /// <summary>Bell.</summary>
    Bell,
    /// <summary>Brackets curly.</summary>
    BracketsCurly,
    /// <summary>Calculator.</summary>
    Calculator,
    /// <summary>Cards.</summary>
    Cards,
    /// <summary>Chart bar horizontal.</summary>
    ChartBarHorizontal,
    /// <summary>Chat.</summary>
    Chat,
    /// <summary>Check circle.</summary>
    CheckCircle,
    /// <summary>Chevron up.</summary>
    ChevronUp,
    /// <summary>Copy.</summary>
    Copy,
    /// <summary>Database.</summary>
    Database,
    /// <summary>Delete.</summary>
    Delete,
    /// <summary>Diamond.</summary>
    Diamond,
    /// <summary>Dice.</summary>
    Dice,
    /// <summary>Document.</summary>
    Document,
    /// <summary>Download.</summary>
    Download,
    /// <summary>Edit.</summary>
    Edit,
    /// <summary>Error.</summary>
    Error,
    /// <summary>Flask.</summary>
    Flask,
    /// <summary>Function (f of x).</summary>
    Function,
    /// <summary>Globe.</summary>
    Globe,
    /// <summary>Globe showing the western hemisphere.</summary>
    GlobeHemisphereWest,
    /// <summary>Graduation cap.</summary>
    GraduationCap,
    /// <summary>Graph.</summary>
    Graph,
    /// <summary>Home.</summary>
    Home,
    /// <summary>Hourglass.</summary>
    Hourglass,
    /// <summary>Image.</summary>
    Image,
    /// <summary>Layers.</summary>
    Layers,
    /// <summary>Map pin.</summary>
    MapPin,
    /// <summary>Map pin line.</summary>
    MapPinLine,
    /// <summary>Medal.</summary>
    Medal,
    /// <summary>Monitor.</summary>
    Monitor,
    /// <summary>Numbered list.</summary>
    NumberedList,
    /// <summary>Offline state.</summary>
    Offline,
    /// <summary>Open in a new window or an external application.</summary>
    OpenExternal,
    /// <summary>Package.</summary>
    Package,
    /// <summary>Palette.</summary>
    Palette,
    /// <summary>Path.</summary>
    Path,
    /// <summary>Pattern.</summary>
    Pattern,
    /// <summary>Picture in picture.</summary>
    PictureInPicture,
    /// <summary>Pin.</summary>
    Pin,
    /// <summary>Play.</summary>
    Play,
    /// <summary>Prohibit.</summary>
    Prohibit,
    /// <summary>Refresh.</summary>
    Refresh,
    /// <summary>Minus sign, used to remove.</summary>
    Remove,
    /// <summary>Reset.</summary>
    Reset,
    /// <summary>Restore.</summary>
    Restore,
    /// <summary>Rows.</summary>
    Rows,
    /// <summary>Ruler.</summary>
    Ruler,
    /// <summary>Save.</summary>
    Save,
    /// <summary>Search.</summary>
    Search,
    /// <summary>Settings.</summary>
    Settings,
    /// <summary>Shield check.</summary>
    ShieldCheck,
    /// <summary>Sidebar.</summary>
    Sidebar,
    /// <summary>Sliders.</summary>
    Sliders,
    /// <summary>Sparkle.</summary>
    Sparkle,
    /// <summary>Star.</summary>
    Star,
    /// <summary>Filled star.</summary>
    StarFilled,
    /// <summary>Swap.</summary>
    Swap,
    /// <summary>Sync.</summary>
    Sync,
    /// <summary>Tag.</summary>
    Tag,
    /// <summary>Text size.</summary>
    TextSize,
    /// <summary>Appearance that follows the system.</summary>
    ThemeSystem,
    /// <summary>Timer.</summary>
    Timer,
    /// <summary>Upgrade.</summary>
    Upgrade,
    /// <summary>Upload.</summary>
    Upload,
    /// <summary>User.</summary>
    User,
    /// <summary>User focus.</summary>
    UserFocus,
    /// <summary>Eye.</summary>
    Eye,
    /// <summary>Git commit.</summary>
    GitCommit,
    /// <summary>Lock.</summary>
    Lock,
    /// <summary>Rocket launch.</summary>
    RocketLaunch,
    /// <summary>Users.</summary>
    Users,
    /// <summary>Stop.</summary>
    Stop,
    /// <summary>List checks.</summary>
    ListChecks,
    /// <summary>Folder.</summary>
    Folder,
    /// <summary>Question.</summary>
    Question,
    /// <summary>Four squares in a grid.</summary>
    SquaresFour,
    /// <summary>Terminal window.</summary>
    TerminalWindow,
    /// <summary>Wrench.</summary>
    Wrench,
    /// <summary>Key.</summary>
    Key,
    /// <summary>Buildings.</summary>
    Buildings,
    /// <summary>Heartbeat.</summary>
    Heartbeat,
    /// <summary>Cloud arrow up.</summary>
    CloudArrowUp,
    /// <summary>Link.</summary>
    Link,
    /// <summary>User plus.</summary>
    UserPlus,
    /// <summary>Layout.</summary>
    Layout,
    /// <summary>Broadcast.</summary>
    Broadcast,
    /// <summary>Calendar blank.</summary>
    CalendarBlank,
    /// <summary>Chart line up.</summary>
    ChartLineUp,
    /// <summary>Folder open.</summary>
    FolderOpen,
    /// <summary>Git fork.</summary>
    GitFork,
    /// <summary>Git merge.</summary>
    GitMerge,
    /// <summary>Lock key.</summary>
    LockKey,
    /// <summary>Robot.</summary>
    Robot,
    /// <summary>Arrow down.</summary>
    ArrowDown,
    /// <summary>Brain.</summary>
    Brain,
    /// <summary>Cell tower.</summary>
    CellTower,
    /// <summary>Cloud.</summary>
    Cloud,
    /// <summary>Gavel.</summary>
    Gavel,
    /// <summary>Git diff.</summary>
    GitDiff,
    /// <summary>Broken link.</summary>
    LinkBreak,
    /// <summary>Lock open.</summary>
    LockOpen,
    /// <summary>Paper plane right.</summary>
    PaperPlaneRight,
    /// <summary>Plugs connected.</summary>
    PlugsConnected,
    /// <summary>Puzzle piece.</summary>
    PuzzlePiece,
    /// <summary>Shield warning.</summary>
    ShieldWarning,
    /// <summary>Sign out.</summary>
    SignOut,
    /// <summary>Arrow elbow down left.</summary>
    ArrowElbowDownLeft,
    /// <summary>Broom.</summary>
    Broom,
    /// <summary>Bug.</summary>
    Bug,
    /// <summary>Chart line.</summary>
    ChartLine,
    /// <summary>Chevron right.</summary>
    ChevronRight,
    /// <summary>Code.</summary>
    Code,
    /// <summary>Gauge.</summary>
    Gauge,
    /// <summary>Magnifying glass with a plus (zoom in).</summary>
    MagnifyingGlassPlus,
    /// <summary>Password.</summary>
    Password,
    /// <summary>Shield struck through.</summary>
    ShieldSlash,
    /// <summary>Stamp.</summary>
    Stamp,
    /// <summary>Text align left.</summary>
    TextAlignLeft,
    /// <summary>Wi-Fi signal at full strength.</summary>
    WifiHigh,
    /// <summary>Arrow elbow down right.</summary>
    ArrowElbowDownRight,
    /// <summary>Arrow line down.</summary>
    ArrowLineDown,
    /// <summary>Arrow line up.</summary>
    ArrowLineUp,
    /// <summary>U-turn arrow pointing right (redo).</summary>
    ArrowUUpRight,
    /// <summary>At sign (@).</summary>
    At,
    /// <summary>Bell struck through (notifications off).</summary>
    BellSlash,
    /// <summary>Books.</summary>
    Books,
    /// <summary>Calendar check.</summary>
    CalendarCheck,
    /// <summary>Double check mark.</summary>
    Checks,
    /// <summary>Cloud arrow down.</summary>
    CloudArrowDown,
    /// <summary>Columns.</summary>
    Columns,
    /// <summary>Corners out.</summary>
    CornersOut,
    /// <summary>Three vertical dots (overflow menu).</summary>
    DotsThreeVertical,
    /// <summary>Eye struck through (hidden).</summary>
    EyeSlash,
    /// <summary>Frame corners.</summary>
    FrameCorners,
    /// <summary>Hard drive.</summary>
    HardDrive,
    /// <summary>Lightning.</summary>
    Lightning,
    /// <summary>Magnifying glass with a minus (zoom out).</summary>
    MagnifyingGlassMinus,
    /// <summary>Megaphone.</summary>
    Megaphone,
    /// <summary>Sign in.</summary>
    SignIn,
    /// <summary>Target.</summary>
    Target,
    /// <summary>Toggle switch in the off position.</summary>
    ToggleLeft,
    /// <summary>Tray.</summary>
    Tray,
    /// <summary>Bold text.</summary>
    TextB,
    /// <summary>Text italic.</summary>
    TextItalic,
    /// <summary>Text underline.</summary>
    TextUnderline,
    /// <summary>Text strikethrough.</summary>
    TextStrikethrough,
    /// <summary>Text subscript.</summary>
    TextSubscript,
    /// <summary>Text superscript.</summary>
    TextSuperscript,
    /// <summary>List bullets.</summary>
    ListBullets,
    /// <summary>Text indent.</summary>
    TextIndent,
    /// <summary>Text outdent.</summary>
    TextOutdent,
    /// <summary>Quotes.</summary>
    Quotes,
    /// <summary>Code block.</summary>
    CodeBlock,
    /// <summary>Text align center.</summary>
    TextAlignCenter,
    /// <summary>Text align right.</summary>
    TextAlignRight,
    /// <summary>Text align justify.</summary>
    TextAlignJustify,
    /// <summary>U-turn arrow pointing left (undo).</summary>
    ArrowUUpLeft,
    /// <summary>Eraser.</summary>
    Eraser,
    /// <summary>HTML file.</summary>
    FileHtml,
    /// <summary>Table.</summary>
    Table,
    /// <summary>Processor (CPU).</summary>
    Cpu,
    /// <summary>File plus.</summary>
    FilePlus,
    /// <summary>Hand tap.</summary>
    HandTap,
    /// <summary>Pause.</summary>
    Pause,
    /// <summary>Toggle switch in the on position.</summary>
    ToggleRight,
    /// <summary>Webhook.</summary>
    Webhook,
    /// <summary>Wi-Fi signal struck through.</summary>
    WifiSlash,
    /// <summary>Windows logo.</summary>
    WindowsLogo,
    /// <summary>Scissors.</summary>
    Scissors,
    /// <summary>Clipboard.</summary>
    Clipboard,
    /// <summary>Thumbs up.</summary>
    ThumbsUp,
    /// <summary>Thumbs down.</summary>
    ThumbsDown,
    /// <summary>Lightbulb.</summary>
    Lightbulb,
    /// <summary>Keyboard.</summary>
    Keyboard,
    /// <summary>Flag.</summary>
    Flag,
    /// <summary>Highlighter.</summary>
    Highlighter,
    /// <summary>Chart pie.</summary>
    ChartPie,
    /// <summary>Chart donut.</summary>
    ChartDonut,
    /// <summary>Circle.</summary>
    Circle,
    /// <summary>Bookmark.</summary>
    Bookmark,
    /// <summary>User minus.</summary>
    UserMinus,
    /// <summary>Selection struck through (clear selection).</summary>
    SelectionSlash,
    /// <summary>Smiley.</summary>
    Smiley,
    /// <summary>Skip back.</summary>
    SkipBack,
    /// <summary>Skip forward.</summary>
    SkipForward,
    /// <summary>Shuffle.</summary>
    Shuffle,
    /// <summary>Repeat.</summary>
    Repeat,
    /// <summary>Repeat once.</summary>
    RepeatOnce,
    /// <summary>Speaker high.</summary>
    SpeakerHigh,
    /// <summary>Speaker low.</summary>
    SpeakerLow,
    /// <summary>Speaker without sound waves.</summary>
    SpeakerNone,
    /// <summary>Speaker muted.</summary>
    SpeakerX,
    /// <summary>Queue.</summary>
    Queue,
    /// <summary>Playlist.</summary>
    Playlist,
    /// <summary>List plus.</summary>
    ListPlus,
    /// <summary>Music note.</summary>
    MusicNote,
    /// <summary>Disc.</summary>
    Disc,
    /// <summary>Waveform.</summary>
    Waveform,
    /// <summary>Six dots in two columns (drag handle).</summary>
    DotsSixVertical,
    /// <summary>Corners in.</summary>
    CornersIn,
    /// <summary>Play circle.</summary>
    PlayCircle,
    /// <summary>Pause circle.</summary>
    PauseCircle,
    /// <summary>Cross in a circle.</summary>
    XCircle,
    /// <summary>Chart bar.</summary>
    ChartBar,
    /// <summary>Book open.</summary>
    BookOpen,
    /// <summary>Arrows pointing down and up.</summary>
    ArrowsDownUp,
    /// <summary>Folder plus.</summary>
    FolderPlus,
    /// <summary>Television.</summary>
    Television,
    /// <summary>Seal check.</summary>
    SealCheck,
    /// <summary>Check square.</summary>
    CheckSquare,
    /// <summary>Arrow bend up right.</summary>
    ArrowBendUpRight,
    /// <summary>Stethoscope.</summary>
    Stethoscope,
    /// <summary>Chevron left.</summary>
    ChevronLeft,
    /// <summary>Envelope.</summary>
    Envelope,
    /// <summary>Euro currency sign.</summary>
    CurrencyEur,
    /// <summary>Share network.</summary>
    ShareNetwork,
    /// <summary>Bank.</summary>
    Bank,
    /// <summary>Briefcase.</summary>
    Briefcase,
    /// <summary>Identification badge.</summary>
    IdentificationBadge,
    /// <summary>PDF file.</summary>
    FilePdf,
    /// <summary>Headset.</summary>
    Headset,
    /// <summary>Handshake.</summary>
    Handshake,
    /// <summary>Arrow up.</summary>
    ArrowUp,
    /// <summary>Markdown file (the letters M and D on a page): the Markdown export of a table.</summary>
    FileMd,
    /// <summary>CSV file: the CSV export of a table.</summary>
    FileCsv,
    /// <summary>Spreadsheet file (XLS): the Excel export of a table.</summary>
    FileXls,
    /// <summary>Word processing file (DOC): a Word document, an export to Word.</summary>
    FileDoc,
    /// <summary>Leaf: ecology, environmental footprint.</summary>
    Leaf
}
