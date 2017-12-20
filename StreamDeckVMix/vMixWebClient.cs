using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.Web;


//
//  All credit for the initial code for this goes to Tim Rode, https://github.com/Tim-R/vScheduler
//

namespace StreamDeckVMix
{
#pragma warning disable IDE1006 // Naming Styles
    public class vMixWebClient
#pragma warning restore IDE1006 // Naming Styles
    {
        WebClient vMix;

        public string URL { get { return vMix.BaseAddress; } set { vMix.BaseAddress = value; } }
        public List<vMixInput> vMixInputs;
        public List<OverlayStatus> OverlayStatus;
        public int ActiveInput;
        public int PreviewInput;
        public Boolean Recording;
        public Boolean Streaming;
        private Boolean FirstUpdate = true;

        public int OldActive = -1;
        public int OldPreview = -1;
        public Boolean ActiveChanged = false;
        public Boolean PreviewChanged = false;


        public vMixWebClient(string baseadress)
        {
            vMix = new WebClient();
            vMix.BaseAddress = baseadress;
            vMixInputs = new List<vMixInput>();
            OverlayStatus = new List<OverlayStatus>();
        }

        public bool GetStatus()
        {
            vMixInputs.Clear();
            OverlayStatus.Clear();
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(vMix.DownloadString("api"));

                string[] v = doc.SelectNodes("vmix/version")[0].InnerText.Split('.');
                if (int.Parse(v[0]) < 11)
                    return false;

                var tmp = Convert.ToInt16(doc.SelectNodes("vmix/active")[0].InnerText);
                if ((tmp != ActiveInput) && !(FirstUpdate))
                {
                    OldActive = ActiveInput;
                    ActiveInput = tmp;
                    ActiveChanged = true;
                }
                else
                {
                    ActiveInput = tmp;
                }

                tmp = Convert.ToInt16(doc.SelectNodes("vmix/preview")[0].InnerText);
                if ((tmp != PreviewInput) && !(FirstUpdate))
                {
                    OldPreview = PreviewInput;
                    PreviewInput = tmp;
                    PreviewChanged = true;
                }
                else
                {
                    PreviewInput = tmp;
                }

                Recording = Convert.ToBoolean(doc.SelectNodes("vmix/recording")[0].InnerText);
                Streaming = Convert.ToBoolean(doc.SelectNodes("vmix/streaming")[0].InnerText);

                foreach (XmlNode node in doc.SelectNodes("vmix/overlays/overlay"))
                {
                    OverlayStatus overlay = new OverlayStatus();
                    overlay.number = int.Parse(node.Attributes.GetNamedItem("number").Value);
                    

                    if (node.InnerText == "")
                    {
                        overlay.Enabled = false;
                    } else //if (Convert.ToInt16(node.InnerText) == overlay.number)
                    {
                        overlay.InputNumber = Convert.ToInt16(node.InnerText);
                        overlay.Enabled = true;
                    }
                    OverlayStatus.Add(overlay);
                }

                foreach (XmlNode node in doc.SelectNodes("vmix/inputs/input"))
                {
                    vMixInput vmi = new vMixInput();
                    vmi.guid = node.Attributes.GetNamedItem("key").Value;
                    vmi.number = int.Parse(node.Attributes.GetNamedItem("number").Value);
                    vmi.type = node.Attributes.GetNamedItem("type").Value;
                    vmi.state = node.Attributes.GetNamedItem("state").Value;
                    vmi.position = int.Parse(node.Attributes.GetNamedItem("position").Value);
                    vmi.duration = int.Parse(node.Attributes.GetNamedItem("duration").Value);
                    vmi.name = node.InnerText;
                    vMixInputs.Add(vmi);
                }

                FirstUpdate = false;
                return true;
            }
            catch (Exception ex) { /*Console.WriteLine(ex); */ return false; }
        }

        public bool GetGUID(string inputname, out string guid)
        {
            guid = "invalid";

            if (!GetStatus())
                return false;

            foreach (vMixInput vmi in vMixInputs)
            {
                if (vmi.name == inputname)
                {
                    guid = vmi.guid;
                    break;
                }
            }

            return true;
        }

        public bool SetActiveInput(int Input)
        {
            try
            {
                vMix.DownloadString("api?function=ActiveInput&Input=" + Input);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool ToggleOverlay(int Input, Boolean status)
        {
            try
            {
                if (status)
                {
                    vMix.DownloadString("api?function=OverlayInput" + Input + "In");
                } else {
                    vMix.DownloadString("api?function=OverlayInput" + Input + "Out");
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool SetPreviewInput(int Input)
        {
            try
            {
                vMix.DownloadString("api?function=PreviewInput&Input=" + Input);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool Transition(string type)
        {
            try
            {
                vMix.DownloadString("api?function=" + type);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool ToggleRecord()  //change from toggle?
        {
            try
            {
                vMix.DownloadString("api?function=StartStopRecording");
            }
            catch
            {
                return false;
            }
            return true;
        }
        public bool ToggleStreaming()   //change from toggle?
        {
            try
            {
                vMix.DownloadString("api?function=StartStopStreaming");
            }
            catch
            {
                return false;
            }
            return true;
        }
    }

#pragma warning disable IDE1006 // Naming Styles
    public struct vMixInput
#pragma warning restore IDE1006 // Naming Styles
    {
        public int number;
        public string guid;
        public string type;
        public string state;
        public int position;
        public int duration;
        public string name;
    }

    public struct OverlayStatus
    {
        public int number;
        public int InputNumber;
        public Boolean Enabled;

    }
}