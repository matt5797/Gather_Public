using Gather.Data;
using Newtonsoft.Json;
using OpenVidu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WebSocketSharp;
using System.Linq;

public class Test_Websocket : MonoBehaviour
{

    private RTCPeerConnection peerConnection;
    private MediaStream sourceStream;

    private DelegateOnIceConnectionChange onIceConnectionChange;
    private DelegateOnIceCandidate onIceCandidate;
    private DelegateOnTrack ontrack;
    private DelegateOnNegotiationNeeded onNegotiationNeeded;

    public WebSocket socket;

    /// <summary>
    /// Unique identifier of the local peer.
    /// </summary>
    [Tooltip("Unique identifier of the local peer")]
    public string LocalPeerId;

    /// <summary>
    /// Unique identifier of the remote peer.
    /// </summary>
    [Tooltip("Unique identifier of the remote peer")]
    public string RemotePeerId;
    
    /// <summary>
    /// The Open vidu server to connect to
    /// </summary>
    [Header("Server")]
    [Tooltip("The server to connect to")]
    public string Server = "127.0.0.1";

    [Tooltip("The secret")]
    public string Secret = "secret";

    [Tooltip("The room")]
    public string Room = "room";

    public RawImage receiveImage;
    public AudioSource receiveAudio;
    private MediaStream receiveAudioStream, receiveVideoStream;

    private string EncodedSecret;

    private OpenViduSessionInfo session;
    private OpenViduJoinRoomAnswer joinRoomAnswer;
    private OpenViduPublishVideoAnswer publishVideoAnswer;
    private OpenViduReceiveVideoAnswer receiveVideoAnswer;
    private OpenViduOnIceCandidateAnswer openViduOnIceCandidateAnswer;
    private OrderedDictionary messages;

    RTCSessionDescription receiveSDP;

    OpenViduType lastMessageType;

    private long idMessage = 1;
    bool startConnection = false;
    bool receiveAnswer = false;
    OpenViduParticipantPublishedEvent participantPublished = null;
    public bool isPublisher = false;

    public Camera cam;

    // Start is called before the first frame update
    void Start()
    {        
        if (string.IsNullOrEmpty(Secret))
        {
            throw new ArgumentNullException("Secret");
        }

        byte[] bytesToEncode = Encoding.UTF8.GetBytes("OPENVIDUAPP:" + Secret);
        EncodedSecret = Convert.ToBase64String(bytesToEncode);

        if (string.IsNullOrEmpty(Server))
        {
            throw new ArgumentNullException("ServerAddress");
        }


        // If not explicitly set, default local ID to some unique ID generated by Unity
        if (string.IsNullOrEmpty(LocalPeerId))
        {
            LocalPeerId = SystemInfo.deviceName;
        }
        
        sourceStream = new MediaStream();

        messages = new OrderedDictionary();

        // �׽�Ʈ �� ����
        Test();

        StartCoroutine(Connect());
    }

    private void Update()
    {
        if (startConnection)
        {
            //StartCoroutine(PublishVideo());
            OnNegotiationNeeded();
            startConnection = false;
        }
        if (receiveAnswer)
        {
            StartCoroutine(ReceiveAnswer(receiveSDP));
            receiveAnswer = false;
        }
        if (participantPublished != null)
        {
            StartCoroutine(ReceiveVideo(participantPublished));
            participantPublished = null;
        }
    }
    public RawImage sourceImage;
    void Test()
    {
        var videoTrack = cam.CaptureStreamTrack(VideoSetting.StreamSize.x, VideoSetting.StreamSize.y, 0);
        sourceImage.texture = videoTrack.Texture;
        sourceStream.AddTrack(videoTrack);
    }

    IEnumerator Ping()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);

            long i = idMessage++;
            SendText("{\"jsonrpc\": \"2.0\"," +
             "\"method\": \"ping\"," +
             "\"params\": {" +
            "\"interval\": 5000  }," +
            "\"id\": " + i + " }", (success) => { Debug.Log($"Ping : {success}"); });

            messages.Add(i, OpenViduType.Ping);
        }
    }

    private IEnumerator Connect()
    {
        var cert = new ForceAcceptAll();

        var www = UnityWebRequest.Get($"https://{Server}/api/sessions/" + Room);
        www.certificateHandler = cert;
        www.SetRequestHeader("Authorization", "Basic " + EncodedSecret);
        //www.SetRequestHeader("customSessionId", Room);
        yield return www.SendWebRequest();
        bool sessionOk = false;
        string token = "";
        if (www.isNetworkError)
        {
            Debug.Log("Error While Sending: " + www.error);
        }
        else
        {
            Debug.Log($"Received{www.responseCode}: {www.downloadHandler.text}");
            session = JsonConvert.DeserializeObject<OpenViduSessionInfo>(www.downloadHandler.text);

            sessionOk = true;
        }


        if (www.responseCode == 404)
        {
            Debug.Log("Creating Session");

            www = new UnityWebRequest($"https://{Server}/api/sessions", "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes("{\"customSessionId\": \"" + Room + "\"}");
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);

            www.certificateHandler = cert;
            www.SetRequestHeader("Authorization", "Basic " + EncodedSecret);
            www.SetRequestHeader("Content-Type", "application/json");
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.Log("Error While Sending: " + www.error);
            }
            else
            {
                Debug.Log($"Received{www.responseCode}: {www.downloadHandler.text}");
                sessionOk = true;
            }
        }

        if (sessionOk)
        {
            Debug.Log("Asking for a token");
            www = new UnityWebRequest($"https://{Server}/api/tokens", "POST");

            byte[] jsonToSend;
            if (isPublisher)
                jsonToSend = new System.Text.UTF8Encoding().GetBytes("{\"session\": \"" + Room + "\"}");
            else
                jsonToSend = new System.Text.UTF8Encoding().GetBytes("{\"session\": \"" + Room + "\", \"role\": \"SUBSCRIBER\"}");

            www.certificateHandler = cert;
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            www.SetRequestHeader("Authorization", "Basic " + EncodedSecret);
            www.SetRequestHeader("Content-Type", "application/json");
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.Log("Error While Sending: " + www.error);
            }
            else
            {
                Debug.Log($"Received{www.responseCode}: {www.downloadHandler.text}");
                var t = JsonConvert.DeserializeObject<OpenViduToken>(www.downloadHandler.text);
                token = t.token;
                Debug.Log($"Token :{token}");
            }
        }

        CreateSocket();
        
        //wait for the socket to be ready
        yield return new WaitUntil(() => socket.ReadyState == WebSocketState.Open);
        Debug.LogAssertion("Socket is ready");

        StartCoroutine(Ping());

        long i = idMessage++;
        SendText("{\"jsonrpc\": \"2.0\"," +
         "\"method\": \"joinRoom\"," +
         "\"params\": {" +
         "\"token\": \"" + token + "\"," +
         "\"session\": \"" + Room + "\"," +
         "\"platform\": \"Chrome 76.0.3809.132 on Linux 64-bit\"," +
         //"\"platform\": \"Unity\"," +
         "\"metadata\": \"{clientData: TestClient, isPublisher: " + isPublisher + "}\"," +
        "\"secret\": \"" + Secret + "\", " +
        "\"recorder\": false  }," +
        "\"id\": " + i + " }", (success) => { Debug.Log($"JoinRoom : {success}"); });

        messages.Add(i, OpenViduType.JoinRoom);

        yield return new WaitForSeconds(5);

        Debug.Log($"LocalDescription: {peerConnection.LocalDescription.sdp}");
        Debug.Log($"RemoteDescription: {peerConnection.RemoteDescription.sdp}");
        Debug.Log($"IceConnectionState: {peerConnection.IceConnectionState}");
        Debug.Log($"ConnectionState: {peerConnection.ConnectionState}");
        Debug.Log($"Stats: {peerConnection.GetStats()}");
        Debug.Log($"Senders: {peerConnection.GetSenders().Count()}");
        Debug.Log($"Receivers: {peerConnection.GetReceivers().Count()}");
    }

    void CreateSocket()
    {
        socket = new WebSocket($"wss://{Server}/openvidu");
        socket.OnOpen += Socket_OnOpen;
        socket.OnMessage += Socket_OnMessage;
        socket.OnError += Socket_OnError;
        socket.OnClose += Socket_OnClose;
        socket.Connect();
    }

    protected void DisconnectWebSocket()
    {
        if (socket != null && socket.IsAlive)
        {
            Debug.Log("Disconnect Web Socket");
            socket.CloseAsync();
        }
    }

    private void Socket_OnOpen(object sender, EventArgs e)
    {
        Debug.Log("Socket Opened" + e);
    }
    
    private void Socket_OnClose(object sender, CloseEventArgs e)
    {
        Debug.Log("Socket Closed" + e);
    }
    
    private enum OpenViduType
    {
        None,
        Ping,
        JoinRoom,
        PublishVideo,
        ReceiveVideoFrom,
        OnIceCandidate,
        ReconnectStream,
        SendMessage
    }

    private void Socket_OnMessage(object sender, MessageEventArgs args)
    {
        if (args == null)
        {
            return;
        }

        var json = args.Data;// myArgs.Text;

        var msg = JsonConvert.DeserializeObject<OpenViduMessageJson>(json);
        // if the message is good
        if (msg != null)
        {
            if (!String.IsNullOrEmpty(msg.Method))
            {
                switch (msg.Method)
                {
                    case "iceCandidate":    // ��� �ּ� �´°� ���⵵�ѵ�, �ణ �ٸ��� ���⵵ �ϰ�...
                        Debug.Log("<color=yellow>" + json + "</color>");
                        OpenViduIceCandidateEvent msg2 = JsonConvert.DeserializeObject<OpenViduIceCandidateEvent>(json);
                        var ic = new RTCIceCandidate(new RTCIceCandidateInit
                        {
                            candidate = msg2.Params.Candidate,
                            sdpMid = msg2.Params.SdpMid,
                            sdpMLineIndex = msg2.Params.SdpMLineIndex
                        });
                        peerConnection.AddIceCandidate(ic);
                        break;
                    case "sendMessage":
                        Debug.Log("sendMessage <color=yellow>" + json + "</color>");
                        break;
                    case "participantJoined":
                        Debug.Log("participantJoined <color=yellow>" + json + "</color>");
                        break;
                    case "participantLeft":
                        Debug.Log("participantLeft <color=yellow>" + json + "</color>");
                        break;
                    case "participantEvicted":
                        Debug.Log("participantEvicted <color=yellow>" + json + "</color>");
                        break;
                    case "participantPublished":
                        Debug.Log("participantPublished <color=yellow>" + json + "</color>");
                        break;
                    case "participantUnpublished":
                        Debug.Log("participantUnpublished <color=yellow>" + json + "</color>");
                        break;
                    case "streamPropertyChanged":
                        Debug.Log("streamPropertyChanged <color=yellow>" + json + "</color>");
                        break;
                    case "recordingStarted":
                        Debug.Log("recordingStarted <color=yellow>" + json + "</color>");
                        break;
                    case "recordingStopped":
                        Debug.Log("recordingStopped <color=yellow>" + json + "</color>");
                        break;
                    case "filterEventDispatched":
                        Debug.Log("filterEventDispatched <color=yellow>" + json + "</color>");
                        break;
                    default:
                        Debug.Log("<color=red>" + json + "</color>");
                        break;
                }
            }
            else if (messages.Contains(msg.id))
            {
                //var id = Int32.Parse(msg.Id);
                long id = msg.id;
                OpenViduType messageType = (OpenViduType)messages[id];
                lastMessageType = messageType;

                switch (messageType)
                {
                    case OpenViduType.Ping:
                        break;
                    case OpenViduType.JoinRoom:
                        Debug.Log("JoinRoom <color=yellow>" + json + "</color>");
                        joinRoomAnswer = JsonConvert.DeserializeObject<OpenViduJoinRoomAnswer>(json);
                        LocalPeerId = joinRoomAnswer.result.id;
                        startConnection = true;
                        //CreateLocalPeer();
                        //StartCoroutine(PublishVideo());
                        break;
                    case OpenViduType.PublishVideo:
                        Debug.Log("PublishVideo <color=yellow>" + json + "</color>");
                        publishVideoAnswer = JsonConvert.DeserializeObject<OpenViduPublishVideoAnswer>(json);
                        receiveSDP = new RTCSessionDescription
                        {
                            type = RTCSdpType.Answer,
                            sdp = publishVideoAnswer.Result.SdpAnswer
                        };
                        receiveAnswer = true;
                        //StartCoroutine(ReceiveAnswer(receiveSDP));
                        //sdpAnswer = new WebRTC.SdpMessage { Type = SdpMessageType.Answer, Content = msg2.Result.SdpAnswer };
                        break;
                    case OpenViduType.ReceiveVideoFrom:
                        Debug.Log("ReceiveVideoFrom <color=yellow>" + json + "</color>");
                        receiveVideoAnswer = JsonConvert.DeserializeObject<OpenViduReceiveVideoAnswer>(json);
                        receiveSDP = new RTCSessionDescription
                        {
                            type = RTCSdpType.Answer,
                            sdp = receiveVideoAnswer.Result.SdpAnswer
                        };
                        receiveAnswer = true;
                        break;
                    case OpenViduType.OnIceCandidate:
                        Debug.Log("OnIceCandidate <color=yellow>" + json + "</color>");
                        openViduOnIceCandidateAnswer = JsonConvert.DeserializeObject<OpenViduOnIceCandidateAnswer>(json);
                        break;
                    case OpenViduType.ReconnectStream:
                        Debug.Log("ReconnectStream <color=yellow>" + json + "</color>");
                        //reconnectStreamAnswer = JsonConvert.DeserializeObject<OpenViduReconnectStreamAnswer>(json);
                        break;
                    case OpenViduType.SendMessage:
                        Debug.Log("SendMessage <color=yellow>" + json + "</color>");
                        break;
                    default:
                        break;
                }

                //timeSincePollMs = PollTimeMs + 1f; //fast forward next request
            }
        }
    }

    private void Socket_OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogAssertion($"Socket_OnError: {e.Message}");
        DisconnectWebSocket();
    }

    void CreateLocalPeer()
    {
        Debug.LogAssertion("CreateLocalPeer");
        peerConnection = new RTCPeerConnection(ref NetworkSetting.rtcConfiguration);

        peerConnection.OnIceCandidate = OnIceCandidate;
        peerConnection.OnIceGatheringStateChange = state =>
        {
            Debug.Log($"2 {GetInstanceID()} OnIceGatheringStateChange {state}");
        };
        peerConnection.OnIceConnectionChange = OnIceConnectionChange;
        peerConnection.OnTrack = OnTrack;
        peerConnection.OnConnectionStateChange = state =>
        {
            Debug.LogAssertion($"{GetInstanceID()} OnConnectionStateChange: {state}");
        };
        peerConnection.OnNegotiationNeeded = OnNegotiationNeeded;
    }

    public string sender;

    void OnNegotiationNeeded()
    {
        if (peerConnection != null)
        {
            StartCoroutine(ReconnectStream());
        }
        else
        {
            if (isPublisher)
                StartCoroutine(PublishVideo());
            /*else
                StartCoroutine(ReceiveVideo());*/
        }
    }

    IEnumerator ReconnectStream()
    {
        print("Start ReconnectStream");

        var op = peerConnection.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (peerConnection.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError($"{GetInstanceID()} signaling state is not stable.");
                yield break;
            }
            RTCSessionDescription desc = op.Desc;

            var op2 = peerConnection.SetLocalDescription(ref desc);
            yield return op2;

            var streamId = isPublisher ? publishVideoAnswer.Result.Id : joinRoomAnswer.result.value[0].streams[0].Id;


            if (!op2.IsError)
            {
                long i = idMessage++;

                var rpcMessage = "{\"jsonrpc\": \"2.0\"," +
                    "\"method\": \"reconnectStream\", " +
                    "\"params\": { " +
                    "\"sdpOffer\": \"" +
                    desc.sdp +
                    "\"," +
                    "\"stream\": " +
                    streamId +
                    "}, \"id\": " +
                   i +
                    " }";

                Debug.Log("SdpMessage: " + rpcMessage);

                SendText(rpcMessage, (success) => { Debug.Log($"reconnectStream : {success}"); });
                messages.Add(i, OpenViduType.ReconnectStream);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    IEnumerator PublishVideo()
    {
        print("Start PublishVideo");
        //if (peerConnection != null)
        //    yield break;

        if (peerConnection == null)
            CreateLocalPeer();

        var op = peerConnection.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (peerConnection.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError($"{GetInstanceID()} signaling state is not stable.");
                yield break;
            }
            RTCSessionDescription desc = op.Desc;

            var op2 = peerConnection.SetLocalDescription(ref desc);
            yield return op2;

            if (!op2.IsError)
            {
                long i = idMessage++;

                var rpcMessage = "{\"jsonrpc\": \"2.0\"," +
                    "\"method\": \"publishVideo\", " +
                    "\"params\": { " +
                    "\"sdpOffer\": \"" +
                    desc.sdp +
                    "\"," +
                    "\"doLoopback\": false," +
                    "\"hasAudio\": true," +
                    "\"hasVideo\": true," +
                    "\"audioActive\": true," +
                    "\"videoActive\": true," +
                    "\"typeOfVideo\": \"CAMERA\"," +
                    "\"frameRate\": 30," +
                    "\"videoDimensions\": \"{\\\"width\\\":" + VideoSetting.StreamSize.x + ",\\\"height\\\":" + VideoSetting.StreamSize.y + "}\"" + //TODO setup video dimensions according to capabilites
                    "}, \"id\": " +
                   i +
                    " }";

                Debug.Log("SdpMessage: " + rpcMessage);

                SendText(rpcMessage, (success) => { Debug.Log($"publishVideo : {success}"); });
                messages.Add(i, OpenViduType.PublishVideo);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    IEnumerator ReceiveVideo()
    {
        print("Start ReceiveVideo");
        /*if (peerConnection != null)
            yield break;*/
        if (peerConnection == null)
            CreateLocalPeer();

        var op = peerConnection.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (peerConnection.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError($"{GetInstanceID()} signaling state is not stable.");
                yield break;
            }
            RTCSessionDescription desc = op.Desc;

            var op2 = peerConnection.SetLocalDescription(ref desc);
            yield return op2;

            if (!op2.IsError)
            {
                long i = idMessage++;

                RemotePeerId = joinRoomAnswer.result.value[0].id;
                var rpcMessage = "{\"jsonrpc\": \"2.0\"," +
                    "\"method\": \"receiveVideoFrom\", " +
                    "\"params\": { " +
                    "\"sdpOffer\": \"" +
                    desc.sdp +
                    "\"," +
                    "\"sender\": " +
                    //sender +
                    joinRoomAnswer.result.value[0].streams[0].Id +
                    "}, \"id\": " +
                   i +
                    " }";

                Debug.Log("SdpMessage: " + rpcMessage);

                SendText(rpcMessage, (success) => { Debug.Log($"receiveVideo : {success}"); });
                messages.Add(i, OpenViduType.ReceiveVideoFrom);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    IEnumerator ReceiveVideo(OpenViduParticipantPublishedEvent publishedEvent)
    {
        print("Start ReceiveVideo");
        /*if (peerConnection != null)
            yield break;*/
        if (peerConnection == null)
            CreateLocalPeer();

        var op = peerConnection.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (peerConnection.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError($"{GetInstanceID()} signaling state is not stable. state: {peerConnection.SignalingState}");
                yield break;
            }
            RTCSessionDescription desc = op.Desc;

            var op2 = peerConnection.SetLocalDescription(ref desc);
            yield return op2;

            if (!op2.IsError)
            {
                long i = idMessage++;

                RemotePeerId = joinRoomAnswer.result.value[0].id;
                var rpcMessage = "{\"jsonrpc\": \"2.0\"," +
                    "\"method\": \"receiveVideoFrom\", " +
                    "\"params\": { " +
                    "\"sdpOffer\": \"" +
                    desc.sdp +
                    "\"," +
                    "\"sender\": " +
                    //sender +
                    //joinRoomAnswer.result.value[0].streams[0].Id +
                    publishedEvent.Params.id +
                    "}, \"id\": " +
                   i +
                    " }";

                Debug.Log("SdpMessage: " + rpcMessage);

                SendText(rpcMessage, (success) => { Debug.Log($"receiveVideo : {success}"); });
                messages.Add(i, OpenViduType.ReceiveVideoFrom);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    IEnumerator ReceiveAnswer(RTCSessionDescription desc)
    {
        var op2 = peerConnection.SetRemoteDescription(ref desc);
        yield return op2;
        if (!op2.IsError)
        {
            OnSetRemoteSuccess(peerConnection);
            // �Ѵ� Add tracks ���ִ°� �³�?
            if (isPublisher)
                AddTracks();
            /*else
                AddTracks();*/
        }
        else
        {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

    public void AddTracks()
    {
        var videoSenders = new List<RTCRtpSender>();
        foreach (var track in sourceStream.GetTracks())
        {
            var sender = peerConnection.AddTrack(track, sourceStream);

            if (track.Kind == TrackKind.Video)
            {
                videoSenders.Add(sender);
            }
        }

        if (VideoSetting.UseVideoCodec != null)
        {
            var codecs = new[] { VideoSetting.UseVideoCodec };
            foreach (var transceiver in peerConnection.GetTransceivers())
            {
                if (videoSenders.Contains(transceiver.Sender))
                {
                    transceiver.SetCodecPreferences(codecs);
                }
            }
        }

        Debug.Log($"{GetInstanceID()} AddTracks: {videoSenders.Count}");
    }

    void OnTrack(RTCTrackEvent e)
    {
        Debug.Log("OnTrack");
        if (e.Track is VideoStreamTrack video)
        {
            Debug.Log("OnTrack video");
            receiveImage.gameObject.SetActive(true);
            video.OnVideoReceived += tex =>
            {
                receiveImage.texture = tex;
            };
            IEnumerable<MediaStream> streams = e.Streams;
            if (streams.Count() > 0)
            {
                receiveVideoStream = streams.First();
                receiveVideoStream.OnRemoveTrack = ev =>
                {
                    //receiveImage.texture = null;
                    ev.Track.Dispose();
                };
            }
        }

        if (e.Track is AudioStreamTrack audioTrack)
        {
            Debug.Log("OnTrack audio");
            receiveAudio.gameObject.SetActive(true);
            receiveAudio.SetTrack(audioTrack);
            receiveAudio.loop = true;
            receiveAudio.Play();
            receiveAudioStream = e.Streams.First();
            receiveAudioStream.OnRemoveTrack = ev =>
            {
                receiveAudio.Stop();
                receiveAudio.clip = null;
                ev.Track.Dispose();
            };
        }
    }

    void SendMessage()
    {
        if (peerConnection == null)
            return;
        if (!socket.IsAlive)
            return;

        long i = idMessage++;
        
        var message = "{\"jsonrpc\": \"2.0\"," +
            "\"method\": \"sendMessage\", " +
            "\"params\": { " +
            "\"message\": " +
            "{\"to\":[],\"data\":\"Test message\",\"type\":\"signal:chat\"}" +
            "}, \"id\": " +
           i +
            " }";

        Debug.Log("SendMessage: " + message);

        SendText(message, (success) => { Debug.Log($"SendMessage : {success}"); });
        messages.Add(i, OpenViduType.SendMessage);
    }

    void OnIceCandidate(RTCIceCandidate candidate)
    {
        long i = idMessage++;
        string iceMessage = "{\"jsonrpc\": \"2.0\"," +
            "\"method\": \"onIceCandidate\", " +
            "\"params\": { " +
            "\"endpointName\":\"" + this.LocalPeerId + "\"," +
            "\"candidate\": \"" + candidate.Candidate + "\"," +
            "\"sdpMid\": \"" + candidate.SdpMid + "\"," +
            "\"sdpMLineIndex\": " + candidate.SdpMLineIndex +
            "}, \"id\": " + i + " }";
        Debug.Log("<color=cyan>IceCandidate:</color> " + iceMessage);
        SendText(iceMessage);
        messages.Add(i, OpenViduType.OnIceCandidate);
    }

    private void OnSetLocalSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetInstanceID()} SetLocalDescription complete");
    }

    static void OnSetSessionDescriptionError(ref RTCError error)
    {
        Debug.LogError($"Error Detail Type: {error.message}");
    }

    private void OnSetRemoteSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetInstanceID()} SetRemoteDescription complete");
    }

    private static void OnCreateSessionDescriptionError(RTCError error)
    {
        Debug.LogError($"Error Detail Type: {error.message}");
    }

    private void OnIceConnectionChange(RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.LogAssertion($"{GetInstanceID()} IceConnectionState: Max");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public void SendText(string text, Action<bool> callback = null)
    {
        if (socket == null || socket.ReadyState != WebSocketState.Open)
        {
            Debug.LogWarning("Web socket is not available to send text message. Try connecting?");
            return;
        }
        //Debug.Log("SendText: " + text);
        socket.SendAsync(text, callback);
    }
}

public class ForceAcceptAll : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}

public class TextEventArgs : EventArgs
{
    public string Text { get; private set; }

    public TextEventArgs(string text)
    {
        this.Text = text;
    }
}