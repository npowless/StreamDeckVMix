using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using StreamDeckSharp;


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

        static void Main(string[] args)
        {
            /////////////////////////
            //   -key locations-   //
            //    4  3  2  1  0    //
            //    9  8  7  6  5    //
            //   14 13 12 11 10    //
            /////////////////////////
            
            Console.CancelKeyPress += Console_CancelKeyPress;
            

            using (var deck = StreamDeck.FromHID())
            {
                deck.SetBrightness(100);

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
                UpdateKeyStatus(deck, true);  //first update
                Console.WriteLine("Connected.  Update key status.");
                deck.KeyPressed += Deck_KeyPressed;
                exitSignal.WaitOne();
            }
        }

        private static void UpdateKeyStatus(object sender, Boolean init = false)
        {
            var d = sender as IStreamDeck;
            if (d == null) return;
            if (WebClient.GetStatus()==false)
            {
                Console.WriteLine("vMix Connection Failed.");
                return;
            }
            
            //first time, update key status
            if (init)
            {
                var AK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.ActiveInput) && (Keys.KeyType == KEY_OUTPUT));
                var PK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.PreviewInput) && (Keys.KeyType == KEY_PREVIEW));
                d.SetKeyBitmap(Keys[AK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[AK_Index].IconEnabled));
                d.SetKeyBitmap(Keys[PK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[PK_Index].IconEnabled));
            }

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
            //update active
           if (WebClient.ActiveChanged)
            {
                var AK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.ActiveInput) && (Keys.KeyType == KEY_OUTPUT));
                var oAK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.OldActive) && (Keys.KeyType == KEY_OUTPUT));
                d.SetKeyBitmap(Keys[AK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[AK_Index].IconEnabled));
                d.SetKeyBitmap(Keys[oAK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[oAK_Index].IconDisabled));
                WebClient.ActiveChanged = false;
            }
            //update preview
            if (WebClient.PreviewChanged)
            {
                var PK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.PreviewInput) && (Keys.KeyType == KEY_PREVIEW));
                var oPK_Index = Keys.FindIndex(Keys => (Keys.Input == WebClient.OldPreview) && (Keys.KeyType == KEY_PREVIEW));
                d.SetKeyBitmap(Keys[PK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[PK_Index].IconEnabled));
                d.SetKeyBitmap(Keys[oPK_Index].Loc, StreamDeckKeyBitmap.FromFile(Keys[oPK_Index].IconDisabled));
                WebClient.PreviewChanged = false;
            }
            
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
                        Thread.Sleep(1000);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_RECORD:
                        WebClient.ToggleRecord();
                        Thread.Sleep(1000);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_STREAM:
                        WebClient.ToggleStreaming();
                        Thread.Sleep(1000);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_CUT:
                        WebClient.Transition("Cut");
                        Thread.Sleep(1000);  //add sleep to give time for vmix to return proper status
                        UpdateKeyStatus(d);
                        break;
                    case KEY_QUICKPLAY:
                        WebClient.Transition("QuickPlay");
                        Thread.Sleep(1000);  //add sleep to give time for vmix to return proper status
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
 