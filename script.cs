using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;


public class script : MonoBehaviour {

    public KMAudio Audio;
    public Material buttonMat;
    public MeshRenderer led;
    public TextMesh[] texts = new TextMesh[6];
    public KMSelectable[] selectables = new KMSelectable[5];
    public KMBombInfo Info;
    public KMBombModule Module;

    public AudioClip[] clips = new AudioClip[149]; // i'll be damned
    public Material[] LedColors = new Material[8];

    private int[] limits = { 109, 209, 310, 410, 125, 224, 323, 426 };
    private const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private int line;
    private int current;
    private int destination;
    private string sn;
    private Color whiteColor = new Color(1,1,1,1);
    private Color azureColor = new Color(0,.5f,1,1);
    private Color limeColor = new Color(.5f,1,0,1);
    private Color invisColor = new Color(1,1,1,0);
    private Color blackColor = new Color(0, 0, 0, 1);
    private float UotBottomLimit;
    private float UotUpperLimit;
    private int queue = 0;
    private int uots = 0;
    private int uotTransfer = 0;
    private bool onTrain = false;
    private bool SOLVED = false;
    private bool onTransfer = false;
    private bool highlighted;
    private bool leaveQueue = false;
    private bool START = false;
    private bool soft = false;

    static int ModuleIdCounter = 1;
    int ModuleId;

    // 0 - on the way 100uot, 1 - arrived, narrats speaking, 2 - opened doors 25uot/45uot, 3 - narrats say next station, 4 - doors closing
    private int state = 0;
    private string[] sounds = new string[74]
    {
        "109",
"110",
"111",
"112",
"113",
"114",
"115",
"116",
"117",
"118",
"119",
"120",
"121",
"122",
"123",
"124",
"125",
"209",
"210",
"211",
"212",
"213",
"214",
"215",
"216",
"217",
"218",
"219",
"220",
"221",
"222",
"223",
"224",
"310",
"311",
"312",
"313",
"314",
"315",
"316",
"317",
"318",
"319",
"320",
"321",
"322",
"323",
"410",
"411",
"412",
"413",
"414",
"415",
"416",
"417",
"418",
"419",
"420",
"421",
"422",
"423",
"424",
"425",
"426",
"last",
"line",
"line1",
"line2",
"line3",
"line4",
"mind",
"next",
"station",
"transfer"
    };

    int indexOf(string query)
    {
        int ans = -1;
        for (int i=0; i<74; i++)
        {
            if (string.Compare(query, sounds[i], true) == 0)
            {
                ans = i;
                break;
            }
        }

        return ans;
    }
    IEnumerator playSound(string name, bool eng, bool silent)
    {
        int index = name == "jingle" ? 148 : indexOf(name) + (eng?74:0);
        if (!silent) Audio.PlaySoundAtTransform(clips[index].name, transform);
        yield return new WaitForSeconds(clips[index].length);
    }
    int transfer(int station)
    {
        switch (station)
        {
            case 216: case 314: case 411: case 419: return 1;
            case 116: case 316: case 416: case 426: return 2;
            case 115: case 218: case 413: case 421: return 3;
            case 112: case 119: case 213: case 220: case 312: case 320: return 4;
            default: return 0;
        }
    }
    int transferToStation(int station)
    {
        switch (station)
        {
            case 112: return 411;
            case 115: return 314;
            case 116: return 216;
            case 119: return 419;
            case 213: return 416;
            case 216: return 116;
            case 218: return 316;
            case 220: return 426;
            case 312: return 413;
            case 314: return 115;
            case 316: return 218;
            case 320: return 421;
            case 411: return 112;
            case 413: return 312;
            case 416: return 213;
            case 419: return 119;
            case 421: return 320;
            case 426: return 220;
            default: return 0;
        }
    }
    bool lastStop(int station)
    {
        switch (station)
        {
            case 109: case 125: case 209: case 224: case 310: case 323: return true;
            default: return false;
        }
    }
    bool correctQueue()
    {
        if (current > 400 || queue == 0) return true;
        int ans = -1;
        for (int i = 0; i < 8; i++) if (limits[i] == current) { ans = i; break; }
        return ans == -1 || (ans - 3) * queue < 0;
    }

    IEnumerator uotsAndStates()
    {
        while (!SOLVED)
        {
            switch (state)
            {
                case 0:
                    {
                        if (!onTransfer) led.material = onTrain ? LedColors[1] : LedColors[4];
                        uots = 100;
                        texts[1].text = uots.ToString();
                        for (; uots > -1; uots--)
                        {
                            yield return new WaitForSeconds(Random.Range(UotBottomLimit, UotUpperLimit));
                            texts[1].text = uots.ToString();
                        }
                        state = 1;
                        break;
                    }
                case 1:
                    {
                        if (onTrain)
                        {
                            current = current + queue;
                            if (current == 409) current = 426;
                            if (current == 427) current = 410;
                            texts[0].text = current.ToString();
                        }
                            yield return playSound("station", false, !onTrain);
                            yield return playSound(current.ToString(), false, !onTrain);
                            if (transfer(current) != 0)
                            {
                                yield return playSound("transfer", false, !onTrain);
                                yield return playSound("line" + transfer(current).ToString(), false, !onTrain);
                                yield return playSound("line", false, !onTrain);
                            }


                            yield return playSound(current.ToString(), true, !onTrain);
                            yield return playSound("station", true, !onTrain);
                            if (transfer(current) != 0)
                            {
                                yield return playSound("transfer", true, !onTrain);
                                yield return playSound("line" + transfer(current).ToString(), true, !onTrain);
                                yield return playSound("line", true, !onTrain);
                            }
                            if (lastStop(current))
                            {
                                yield return playSound("last", false, !onTrain);
                                yield return playSound("last", true, !onTrain);
                            }
                        state = 2;
                        break;
                    }
                case 2:
                    {
                        if (!onTransfer) led.material = onTrain ? LedColors[3] : LedColors[2];
                        if (leaveQueue)
                        {
                            onTrain = false;
                            queue = 0;
                            texts[5].color = blackColor;
                            leaveQueue = false;
                            led.material = LedColors[2];
                        }
                        if (queue!=0 && !onTrain && !onTransfer)
                        {
                            onTrain = true;
                            texts[2].color = blackColor;
                            texts[3].color = blackColor;
                            led.material = LedColors[3];
                        }
                        uots = lastStop(current)? 45:25;
                        texts[1].text = uots.ToString();
                        for (; uots > -1; uots--)
                        {
                            yield return new WaitForSeconds(Random.Range(UotBottomLimit, UotUpperLimit));
                            texts[1].text = uots.ToString();
                        }
                        
                        state = 3;
                        break;
                    }
                case 3:
                    {
                        if (onTrain && lastStop(current) && !correctQueue())
                        {
                            Module.HandleStrike();
                            onTrain = false;
                            queue = 0;
                        }
                        else
                        {
                            yield return playSound("next", false, !onTrain);
                            yield return playSound((current + queue == 409 ? 426 : current + queue == 427 ? 410 : current + queue).ToString(), false, !onTrain);
                            yield return playSound("mind", false, !onTrain);
                            yield return playSound("next", true, !onTrain);
                            yield return playSound((current + queue == 409 ? 426 : current + queue == 427 ? 410 : current + queue).ToString(), true, !onTrain);
                            yield return playSound("mind", true, !onTrain);
                        }
                        state = 4;
                        break;
                    }
                case 4:
                    {
                        if (!onTransfer) led.material = onTrain ? LedColors[5] : LedColors[6];
                        yield return playSound("jingle", false, !onTrain);
                        state = 0;
                        break;
                    }
            }
        }
        yield return true;
    }
    IEnumerator transferIE()
    {
        onTransfer = true;
        led.material = LedColors[7];
        if (highlighted)
        {
            texts[1].color = invisColor;
            texts[4].color = limeColor;
        }

        uotTransfer = 40;
        texts[4].text = uotTransfer.ToString();
        for (; uotTransfer > -1; uotTransfer--)
        {
            yield return new WaitForSeconds(Random.Range(UotBottomLimit, UotUpperLimit));
            texts[4].text = uotTransfer.ToString();
        }

        onTransfer = false;
        if (highlighted)
        {
            texts[4].color = invisColor;
            texts[1].color = azureColor;
        }
        current = transferToStation(current);
        texts[0].text = current.ToString();
        switch (state)
        {
            case 0: case 1: led.material = LedColors[4]; break;
            case 2: case 3: led.material = LedColors[2]; break;
            case 4: if (!onTransfer) led.material = LedColors[6]; break;
        }

        yield return true;
    }
    IEnumerator test()
    {
        yield return playSound("station", false, false);
        yield return playSound(current.ToString(), false, false);
        yield return playSound(current.ToString(), true, false);
        yield return playSound("station", true, false);
        yield return true;
    }

    private void Awake()
    {
        ModuleId = ModuleIdCounter++;
    }

    void Start () {
        texts[0].text = "---";
        texts[1].text = "---";
        led.material = LedColors[0];
        int time = (int)Info.GetTime();
        UotBottomLimit = (time != 0 && time<3600)?.8f * time / 3600:.8f;
        UotUpperLimit = 1.5f * UotBottomLimit;
        sn = Info.GetSerialNumber();
        line = Random.Range(1, 5);
        current = Random.Range(limits[line-1], limits[line + 3]);
        int destLine = (line - (base36.IndexOf(sn[0]) / 12) + 2) % 4 + 1;
        destination = limits[destLine + 3] - (base36.IndexOf(sn[2]) * 36 + base36.IndexOf(sn[4])) % (limits[destLine + 3] - limits[destLine - 1] + 1);
        Debug.LogFormat("[Minsk Metro #{0}] Starting station: {1}", ModuleId, current);
        Debug.LogFormat("[Minsk Metro #{0}] Your destination: {1}", ModuleId, destination);

        selectables[1].OnInteract = delegate {
            if (SOLVED || onTransfer) return true;
            if (!START)
            {
                START = true;
                texts[0].text = current.ToString();
                StartCoroutine(test());
                StartCoroutine(uotsAndStates());
                led.material = LedColors[4];
            } else
            if (!onTrain && transferToStation(current) != 0)
            {
                StartCoroutine(transferIE());
            }
            return true; };
        selectables[2].OnInteract = delegate {
            if (SOLVED || !START || onTransfer) return true;
            if (!onTransfer)
            {
                if (onTrain)
                {
                    if (state>1)
                    {
                        onTrain = false;
                        queue = 0;
                        leaveQueue = false;
                        if (state == 4)
                        {
                            led.material = LedColors[6];
                            if (soft) { soft = false; Module.HandleStrike(); } else soft = true;
                        }
                        else led.material = LedColors[2];

                    } else
                    if (leaveQueue)
                    {
                        leaveQueue = false;
                        texts[5].color = blackColor;
                    }
                    else
                    {
                        leaveQueue = true;
                        texts[5].color = azureColor;
                    }
                }
                else
                {
                    if (current == destination)
                    {
                        Debug.LogFormat("[Minsk Metro #{0}] Submitted on {1}, which is correct.", ModuleId, current);
                        SOLVED = true;
                        led.material =LedColors[0];
                        texts[0].text = "---";
                        StartCoroutine(playSound("jingle", false, false));
                        Module.HandlePass();
                    }
                    else
                    {
                        Debug.LogFormat("[Minsk Metro #{0}] Submitted on {1}, which is wrong.", ModuleId, current);
                        Module.HandleStrike();
                    }
                }
            }
            return true;
        };
        selectables[3].OnInteract = delegate {
            if (SOLVED || !START || onTransfer) return true;
            if (!onTrain)
            {
                if (state > 1)
                {
                    onTrain = true;
                    queue = 1;
                    if (state == 4)
                    {
                        led.material = LedColors[5];
                        if (soft) { soft = false; Module.HandleStrike(); } else soft = true;
                    }
                    else led.material = LedColors[3];
                } else
                if (queue == 1)
                {
                    texts[2].color = blackColor;
                    queue = 0;
                }
                else
                {
                    texts[2].color = azureColor;
                    texts[3].color = blackColor;
                    queue = 1;
                }
            }
            return true; };
        selectables[4].OnInteract = delegate {
            if (SOLVED || !START || onTransfer) return true;
            if (!onTrain)
            {
                if (state > 1)
                {
                    onTrain = true;
                    queue = -1;
                    if (state == 4)
                    {
                        led.material = LedColors[5];
                        if (soft) { soft = false; Module.HandleStrike(); } else soft = true;
                    }
                    else led.material = LedColors[3];
                }
                else
                if (queue == -1)
                {
                    texts[3].color = blackColor;
                    queue = 0;
                }
                else
                {
                    texts[3].color = azureColor;
                    texts[2].color = blackColor;
                    queue = -1;
                }
            }
            return true; };

        

        selectables[0].OnHighlight = delegate {
            highlighted = true;
            if (onTransfer)
            {
                texts[0].color = invisColor;
                texts[4].color = limeColor;
            }
            else
            {
                texts[0].color = invisColor;
                texts[1].color = azureColor;
            }
        };
        selectables[0].OnHighlightEnded = delegate {
            highlighted = false;
            if (onTransfer)
            {
                texts[4].color = invisColor;
                texts[0].color = whiteColor;
            }
            else
            {
                texts[1].color = invisColor;
                texts[0].color = whiteColor;
            }
            
        };
	}

#pragma warning disable 414
    private readonly string TwitchHelpMessage = 
        @"Use !{0} up/down/enter/leave to press corresponding buttons. Use !{0} hover to hover/unhover the display.";
    private bool TwitchPlaysActive = false;
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
        Command = Command.ToLower();
        switch (Command)
        {
            case "enter": { selectables[1].OnInteract(); break; }
            case "leave": { selectables[2].OnInteract(); break; }
            case "up": { selectables[3].OnInteract(); break; }
            case "down": { selectables[4].OnInteract(); break; }
            case "hover":
                {
                    if (highlighted) selectables[0].OnHighlightEnded();
                    else selectables[0].OnHighlight();
                    break;
                }
            default: yield return "sendtochaterror Invalid command!"; break;
        }


        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        current = destination;
        texts[0].text = destination.ToString();
        highlighted = false;
        yield return test();
        SOLVED = true;
        led.material = LedColors[0];
        texts[0].text = "---";
        StartCoroutine(playSound("jingle", false, false));
        Module.HandlePass();
        yield return null;
    }
}
