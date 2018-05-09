using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

#if WINDOWS_UWP
using System.Threading.Tasks;
using Windows.Networking.Sockets;
#endif

public class MarkerManager : MonoBehaviour
{


    GestureRecognizer recognizer;
    public byte[] byteArray_Receive { get; private set; }

    public IPEndPoint iep_Recieve { get; private set; }
#if WINDOWS_UWP
	public DatagramSocket socketServer{ get; private set; }
#else
    public UdpClient socketServer { get; private set; }
#endif
    static public Dictionary<string, Vector3> MarkerPositions { get; private set; }

    static public Dictionary<string, Quaternion> MarkerOrientations { get; private set; }

    private Thread receiveThread;

    String port;
    // Use this for initialization

    void Start()
    {
        MarkerOrientations = new Dictionary<string, Quaternion>();
        MarkerPositions = new Dictionary<string, Vector3>();

#if WINDOWS_UWP
		socketServer = new DatagramSocket();

        port = "5302";

        Socket_start();

        socketServer.MessageReceived += SocketMessageReceived;

        socketServer.Control.MulticastOnly = false;


        
  //      await socketServer.BindEndpointAsync(null, "5302");

       

#else
        byteArray_Receive = new byte[100];
        //定义网络地址  
        iep_Recieve = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5302);
        //创建socket  
        socketServer = new UdpClient(5302);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
#endif
    }
#if WINDOWS_UWP
    private async void Socket_start()
    {
        Debug.Log("socket started");
        try
        {
            await socketServer.BindServiceNameAsync("5302");
            Debug.Log("connect");
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }
        Debug.Log("Listening");
    }
#endif

    // Update is called once per frame
    void Update()
    {
     

    }



    void ParseMessage(string input)
    {
        MarkerPositions.Clear();
        MarkerOrientations.Clear();
        string[] records = input.Split(new char[] { '|' }, 10);
        for (int i = 1; i < records.Length; i++)
        {
            string[] data = records[i].Split(new char[] { ',' }, 10);
            MarkerPositions[data[0]] = new Vector3(Convert.ToSingle(data[6]) / 1000f, -Convert.ToSingle(data[5]) / 1000f, -Convert.ToSingle(data[7]) / 1000f);
            MarkerOrientations[data[0]] = new Quaternion(-Convert.ToSingle(data[3]), Convert.ToSingle(data[2]), Convert.ToSingle(data[4]), Convert.ToSingle(data[1]));
        }
    }

    void OnDestroy()
    {
#if WINDOWS_UWP

#else
        receiveThread.Abort();
#endif
    }

#if WINDOWS_UWP
	private async void SocketMessageReceived(DatagramSocket sender,DatagramSocketMessageReceivedEventArgs args){
		Stream streamIn = args.GetDataStream().AsStreamForRead();
		StreamReader reader= new StreamReader(streamIn);
		string message = await reader.ReadLineAsync();
		ParseMessage(message);
	}

#else
    void ReceiveData()
    {
        for (;;)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 5302);
            byteArray_Receive = socketServer.Receive(ref ep);
            string strReceiveStr = new ASCIIEncoding().GetString(byteArray_Receive, 0, byteArray_Receive.Length);
            //print (strReceiveStr);
            ParseMessage(strReceiveStr);
        }
    }
#endif
}
