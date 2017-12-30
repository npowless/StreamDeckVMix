using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StreamDeckSharp;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace StreamDeckVMix
{
    class Program
    {
        static List<KeyList> Keys = new List<KeyList>();
        public const int KEY_PREVIEW = 0;
        public const int KEY_OUTPUT = 1;
        public const int KEY_OVERLAY = 2;
        public const int KEY_STREAM = 3;
        public const int KEY_RECORD = 4;
        public const int KEY_QUICKPLAY = 5;
        public const int KEY_CUT = 6;
        public const string URL = "http://127.0.0.1:8088/";
        private static readonly ManualResetEvent exitSignal = new ManualResetEvent(false);

        static vMixWebClient WebClient = new vMixWebClient(URL);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;


        static NotifyIcon notifyIcon;
        static IntPtr processHandle;
        static IntPtr WinShell;
        static IntPtr WinDesktop;
        static MenuItem HideMenu;
        static MenuItem RestoreMenu;

        static void Main(string[] args)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Stream IconResourceStream = currentAssembly.GetManifestResourceStream("StreamDeckVMix.StreamDeckVMix.ico");
            
            notifyIcon = new NotifyIcon();            
            notifyIcon.Icon = new System.Drawing.Icon(IconResourceStream);
            notifyIcon.Text = "StreamDeck vMix Console";
            notifyIcon.Visible = true;

            ContextMenu menu = new ContextMenu();
            HideMenu = new MenuItem("Hide Console", new EventHandler(Minimize_Click));
            RestoreMenu = new MenuItem("Show Console", new EventHandler(Maximize_Click));

            menu.MenuItems.Add(RestoreMenu);
            menu.MenuItems.Add(HideMenu);
            menu.MenuItems.Add(new MenuItem("Exit", new EventHandler(CleanExit)));

            notifyIcon.ContextMenu = menu;
            notifyIcon.DoubleClick += new System.EventHandler(Maximize_Click); //also show on double click

            Task.Factory.StartNew(Run);

            processHandle = Process.GetCurrentProcess().MainWindowHandle;

            WinShell = GetShellWindow();
            ShowWindowAsync(WinShell, SW_SHOWMINIMIZED);

            WinDesktop = GetDesktopWindow();
            //Hide the Window
            ResizeWindow(false);
            Application.Run();
        }

        static void Run()
        {
            using (var deck = StreamDeck.FromHID())
            {
                deck.SetBrightness(100);

                /////////////////////////
                //   -key locations-   //
                //    4  3  2  1  0    //
                //    9  8  7  6  5    //
                //   14 13 12 11 10    //
                /////////////////////////

                Console.WriteLine("Loading Keys...");

                Keys.Add(new KeyList { KeyName = "Preview 1", KeyType = KEY_PREVIEW, Loc = 14, IconDisabled = "icons\\1_grey.png", IconEnabled = "icons\\1_green.png", Input = 1 });
                Keys.Add(new KeyList { KeyName = "Preview 2", KeyType = KEY_PREVIEW, Loc = 13, IconDisabled = "icons\\2_grey.png", IconEnabled = "icons\\2_green.png", Input = 2 });
                Keys.Add(new KeyList { KeyName = "Preview 3", KeyType = KEY_PREVIEW, Loc = 12, IconDisabled = "icons\\3_grey.png", IconEnabled = "icons\\3_green.png", Input = 3 });
                Keys.Add(new KeyList { KeyName = "Preview 4", KeyType = KEY_PREVIEW, Loc = 11, IconDisabled = "icons\\4_grey.png", IconEnabled = "icons\\4_green.png", Input = 4 });

                Keys.Add(new KeyList { KeyName = "Output 1", KeyType = KEY_OUTPUT, Loc = 9, IconDisabled = "icons\\1_grey.png", IconEnabled = "icons\\1_red.png", Input = 1 });
                Keys.Add(new KeyList { KeyName = "Output 2", KeyType = KEY_OUTPUT, Loc = 8, IconDisabled = "icons\\2_grey.png", IconEnabled = "icons\\2_red.png", Input = 2 });
                Keys.Add(new KeyList { KeyName = "Output 3", KeyType = KEY_OUTPUT, Loc = 7, IconDisabled = "icons\\3_grey.png", IconEnabled = "icons\\3_red.png", Input = 3 });
                Keys.Add(new KeyList { KeyName = "Output 4", KeyType = KEY_OUTPUT, Loc = 6, IconDisabled = "icons\\4_grey.png", IconEnabled = "icons\\4_red.png", Input = 4 });

                Keys.Add(new KeyList { KeyName = "Overlay 1", KeyType = KEY_OVERLAY, Loc = 4, IconDisabled = "icons\\1_grey.png", IconEnabled = "icons\\1_yel.png", Input = 1, Active = false });
                Keys.Add(new KeyList { KeyName = "Overlay 2", KeyType = KEY_OVERLAY, Loc = 3, IconDisabled = "icons\\2_grey.png", IconEnabled = "icons\\2_yel.png", Input = 2, Active = false });
                Keys.Add(new KeyList { KeyName = "Overlay 3", KeyType = KEY_OVERLAY, Loc = 2, IconDisabled = "icons\\3_grey.png", IconEnabled = "icons\\3_yel.png", Input = 3, Active = false });

                Keys.Add(new KeyList { KeyName = "Stream", KeyType = KEY_STREAM, Loc = 1, IconDisabled = "icons\\stream_off.png", IconEnabled = "icons\\stream_on.png" });
                Keys.Add(new KeyList { KeyName = "Record", KeyType = KEY_RECORD, Loc = 0, IconDisabled = "icons\\record_off.png", IconEnabled = "icons\\record_on.png" });
                Keys.Add(new KeyList { KeyName = "Quick Play", KeyType = KEY_QUICKPLAY, Loc = 5, IconDisabled = "icons\\quickplay.png", IconEnabled = "icons\\quickplay.png" });
                Keys.Add(new KeyList { KeyName = "Cut", KeyType = KEY_CUT, Loc = 10, IconDisabled = "icons\\cut.png", IconEnabled = "icons\\cut.png" });

                deck.ClearKeys();

                //Load keys
                foreach (KeyList KeyInit in Keys)
                {
                    //combined += thing.Name;
                    deck.SetKeyBitmap(KeyInit.Loc, StreamDeckKeyBitmap.FromFile(KeyInit.IconDisabled));
                }

                Console.WriteLine("Keys loaded, doing update...");

                Boolean status = UpdateKeyStatus(deck, true);  //first update
                if (status)
                {
                    Console.WriteLine("Connected.  Performing initial key status update.");
                    Console.WriteLine("Waiting for input.");
                }
                else
                {
                    Console.WriteLine("vMix Connection Error, retrying....");
                    while (status == false)
                    {
                        status = UpdateKeyStatus(deck, true);
                    }
                    Console.WriteLine("Connected to vMix.  Waiting for key input.");
                }

                //Console.WriteLine("keypress listen start");
                deck.KeyPressed += Deck_KeyPressed; //listen for keypresses

                while (true)
                {
                    Thread.Sleep(2000);  //two second update to check for changes not triggered by a keypress
                    UpdateKeyStatus(deck);
                    //Console.WriteLine("Update keys LOOP.");
                }
            }
        }
 
        private static Boolean UpdateKeyStatus(object sender, Boolean init = false)
        {
            var d = sender as IStreamDeck;
            if (d == null) return false;
            if (WebClient.GetStatus()==false)
            {
                Console.WriteLine("vMix Connection Failed.");
                return false;
            }
            //Console.WriteLine("UKS-1, Before Init");
            //first time, update key status
            if (init == true)
            {
                if ((WebClient.ActiveInput > 4)) {
                    Console.WriteLine("Current Active Input > 4");
                    var PK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.PreviewInput) && (Keys.KeyType == KEY_PREVIEW));
                    d.SetKeyBitmap(Keys[PK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[PK_Index].IconEnabled));
                } else if (WebClient.PreviewInput > 4) {
                    Console.WriteLine("Current Preview Input > 4");
                    var AK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.ActiveInput) && (Keys.KeyType == KEY_OUTPUT));
                    d.SetKeyBitmap(Keys[AK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[AK_Index].IconEnabled));
                } else {
                    try
                    {
                        var AK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.ActiveInput) && (Keys.KeyType == KEY_OUTPUT));
                        var PK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.PreviewInput) && (Keys.KeyType == KEY_PREVIEW));
                        d.SetKeyBitmap(Keys[AK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[AK_Index].IconEnabled));
                        d.SetKeyBitmap(Keys[PK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[PK_Index].IconEnabled));
                    }
                    catch (Exception ex) { Console.WriteLine(ex); return false; }
                }
            }
            //Console.WriteLine("UKS-2, after INIT");
            //check overlay status
            foreach (OverlayStatus overlay in WebClient.OverlayStatus)
            {
                try
                {
                    var OverlayKey_Index = Keys.FindIndex(Keys => (Keys.Input == overlay.number) && (Keys.KeyType == KEY_OVERLAY));
                    //Console.Write(overlay.number + ": " + overlay.Enabled + ", ");
                    if (overlay.Enabled)
                    {
                        d.SetKeyBitmap(Keys[OverlayKey_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[OverlayKey_Index].IconEnabled));
                        Keys[OverlayKey_Index].Active = true;
                    }
                    else
                    {
                        d.SetKeyBitmap(Keys[OverlayKey_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[OverlayKey_Index].IconDisabled));
                        Keys[OverlayKey_Index].Active = false;
                    }
                }
                catch { }
            }
            //Console.WriteLine("UKS-3, after Overlay");
            //update active
            if (WebClient.ActiveChanged)
            {
                if ((WebClient.ActiveInput > 4))
                {
                    Console.WriteLine("Current Active Input > 4");
                    if (WebClient.OldActive <= 4)
                    {
                        var oAK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.OldActive) && (Keys.KeyType == KEY_OUTPUT));
                        d.SetKeyBitmap(Keys[oAK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[oAK_Index].IconDisabled));
                    }
                }
                else
                {
                    if (WebClient.OldActive <= 4)
                    {
                        var oAK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.OldActive) && (Keys.KeyType == KEY_OUTPUT));
                        d.SetKeyBitmap(Keys[oAK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[oAK_Index].IconDisabled));
                    }
                    var AK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.ActiveInput) && (Keys.KeyType == KEY_OUTPUT));
                    d.SetKeyBitmap(Keys[AK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[AK_Index].IconEnabled));
                }
                WebClient.ActiveChanged = false;
            }
            //Console.WriteLine("UKS-4, After Active");
            //update preview
            if (WebClient.PreviewChanged)
            {
                if ((WebClient.PreviewInput > 4))
                {
                    Console.WriteLine("Current Preview Input > 4");
                    if (WebClient.OldPreview <= 4)
                    {
                        var oPK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.OldPreview) && (Keys.KeyType == KEY_PREVIEW));
                        d.SetKeyBitmap(Keys[oPK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[oPK_Index].IconDisabled));
                    }
                }
                else
                {
                    if (WebClient.OldPreview <= 4)
                    {
                        var oPK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.OldPreview) && (Keys.KeyType == KEY_PREVIEW));
                        d.SetKeyBitmap(Keys[oPK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[oPK_Index].IconDisabled));
                    }
                    var PK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.PreviewInput) && (Keys.KeyType == KEY_PREVIEW));
                    d.SetKeyBitmap(Keys[PK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[PK_Index].IconEnabled));
                }
                WebClient.PreviewChanged = false;
            }
            //Console.WriteLine("UKS-5, After Preview");
            //update recording status
            var RecordKey_Index = Keys.FindIndex(Keys => Keys.KeyType == KEY_RECORD);
            if (WebClient.Recording)
            {
                d.SetKeyBitmap(Keys[RecordKey_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[RecordKey_Index].IconEnabled));
            }
            else
            {
                d.SetKeyBitmap(Keys[RecordKey_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[RecordKey_Index].IconDisabled));
            }
            //Console.WriteLine("UKS-6, After Recording Status");
            //update streaming status
            var StreamKey_Index = Keys.FindIndex(Keys => Keys.KeyType == KEY_STREAM);
            if (WebClient.Streaming)
            {
                d.SetKeyBitmap(Keys[StreamKey_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[StreamKey_Index].IconEnabled));
            }
            else
            {
                d.SetKeyBitmap(Keys[StreamKey_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[StreamKey_Index].IconDisabled));
            }
            //Console.WriteLine("UKS-6, After Streaming Status - DONE");
            return true;
        }

        private static void Deck_KeyPressed(object sender, StreamDeckKeyEventArgs e)
        {
            var d = sender as IStreamDeck;
            if (d == null) return;
            
            if (e.IsDown)
            {
                KeyList CurrentKey = Keys.Find(Keys => Keys.Loc == e.Key);
                var CK_Index = Keys.FindIndex(Keys => Keys.Loc == e.Key);

                Console.WriteLine(CurrentKey.KeyName + " Pressed.");

                switch (CurrentKey.KeyType)
                {
                    case KEY_PREVIEW:
                        WebClient.SetPreviewInput(Keys[CK_Index].Input);
                        UpdateKeyStatus(d);
                        break;
                    case KEY_OUTPUT:
                        WebClient.SetActiveInput(Keys[CK_Index].Input);
                        UpdateKeyStatus(d);
                        break;
                    case KEY_OVERLAY:
                        if (Keys[CK_Index].Active == false)
                        {
                            WebClient.ToggleOverlay(Keys[CK_Index].Input, true);
                        }
                        else
                        {
                            WebClient.ToggleOverlay(Keys[CK_Index].Input, false);
                        }          
                        Thread.Sleep(750);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_RECORD:
                        WebClient.ToggleRecord();
                        Thread.Sleep(750);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_STREAM:
                        WebClient.ToggleStreaming();
                        Thread.Sleep(7500);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_CUT:
                        WebClient.Transition("Cut");
                        Thread.Sleep(750);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_QUICKPLAY:
                        WebClient.Transition("QuickPlay");
                        Thread.Sleep(750);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;

                }
            }
        }
        
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            exitSignal.Set();
            e.Cancel = true;
        }

        private static void CleanExit(object sender, EventArgs e)
        {
            using (var deck = StreamDeck.FromHID())
            {
                deck.ClearKeys();
            }
            notifyIcon.Visible = false;
            Application.Exit();
            Environment.Exit(1);
        }

        private static void MinimizeWindow(object sender, EventArgs e)
        {
            // retrieve Notepad main window handle
            //IntPtr hWnd = FindWindow("StreamDeckVMix");
            //if (!hWnd.Equals(IntPtr.Zero))
            //{
                // SW_SHOWMAXIMIZED to maximize the window
                // SW_SHOWMINIMIZED to minimize the window
                // SW_SHOWNORMAL to make the window be normal size
                //ShowWindowAsync(hWnd, SW_SHOWMINIMIZED);
            ShowWindowAsync(WinShell, SW_SHOWMINIMIZED);
            // }
        }
        static void Minimize_Click(object sender, EventArgs e)
        {
            ResizeWindow(false);
        }


        static void Maximize_Click(object sender, EventArgs e)
        {
            ResizeWindow();
        }

        static void ResizeWindow(bool Restore = true)
        {
            if (Restore == true)
            {
                RestoreMenu.Enabled = false;
                HideMenu.Enabled = true;
                SetParent(processHandle, WinDesktop);
            }
            else
            {
                RestoreMenu.Enabled = true;
                HideMenu.Enabled = false;
                SetParent(processHandle, WinShell);
            }
        }
    }

}


[Serializable]
public class KeyList
{
    public string KeyName { get; set; }
    public int Loc { get; set; }
    public int KeyType { get; set; }
    public string IconDisabled { get; set; }
    public string IconEnabled { get; set; }    
    public Boolean Active { get; set; }
    public int Input { get; set; }
}
 