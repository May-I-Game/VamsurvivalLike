using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLobbyManager : MonoBehaviourPunCallbacks
{
    private static int roomNumber = 1;  // 방 번호를 추적할 static 변수 추가

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject lobbyPanel;
    public GameObject createRoomPanel;

    [Header("Login UI")]
    public InputField nickNameInputField;
    public Button loginButton;

    [Header("Lobby UI")]
    public Button createRoomButton;
    public Text playerText;
    public Text statusText;

    [Header("Create Room UI")]
    public InputField maxPlayersInputField;

    [Header("Room List")]
    public Transform roomGrid;
    public GameObject roomItemPrefab;

    private readonly Dictionary<string, GameObject> roomListEntries = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // 방장이 씬을 바꾸면 나중에 접속한 플레이어들도 동일한 씬으로 로드되도록 설정
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    #region Network Core

    public void ConnectToServer()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("서버 접속 시도 중...");
        }
    }

    public void CreateRoom(string roomName, byte maxPlayers = 5)
    {
        if (PhotonNetwork.IsConnected)
        {
            RoomOptions options = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = true,
                IsOpen = true
            };
            PhotonNetwork.CreateRoom(roomName, options);
            Debug.Log($"방 생성 시도: {roomName}");
        }
    }

    public void JoinRoom(string roomName)
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRoom(roomName);
            Debug.Log($"방 참가 시도: {roomName}");
        }
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    #endregion

    #region UI Methods

    private void SetActivePanel(GameObject panel)
    {
        loginPanel?.SetActive(panel == loginPanel);
        lobbyPanel?.SetActive(panel == lobbyPanel);
        createRoomPanel?.SetActive(panel == createRoomPanel);
    }

    private void ShowStatus(string message, bool isError = false)
    {
        Debug.Log($"[Lobby] {message}");
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.white;
        }
    }

    #endregion

    #region UI Callbacks

    public void OnLoginButtonClicked()
    {
        string nickName = string.IsNullOrEmpty(nickNameInputField.text) ? $"Player_{Random.Range(1000, 10000)}" : nickNameInputField.text;
        PhotonNetwork.NickName = nickName;

        loginButton.interactable = false;
        ShowStatus("서버에 접속 중...");
        ConnectToServer(); // 여기서 호출
    }

    public void OnCreateRoomButtonClicked()
    {
        // 방 생성 버튼 클릭시 패널만 보여줌
        SetActivePanel(createRoomPanel);
        maxPlayersInputField.text = "4";  // 기본값 설정
    }

    // 실제 방 생성 로직 (Create Room Panel의 Create 버튼에 연결)
    public void OnCreateRoomConfirmed()
    {
        string roomName = $"Room_{roomNumber++}";

        byte maxPlayers;
        if (!byte.TryParse(maxPlayersInputField.text, out maxPlayers))
        {
            ShowStatus("최대 플레이어 수를 확인하세요! (2-8)", true);
            return;
        }

        maxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 8);
        CreateRoom(roomName, maxPlayers);
    }

    // Create Room Panel의 Cancel 버튼에 연결
    public void OnCreateRoomCanceled()
    {
        SetActivePanel(lobbyPanel);
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        ShowStatus("서버에 접속됨");

        if (PhotonNetwork.InLobby) return;
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        ShowStatus("로비에 접속됨");
        SetActivePanel(lobbyPanel);
        playerText.text = $"환영합니다. {PhotonNetwork.NickName}님";
    }

    public override void OnCreatedRoom()
    {
        ShowStatus("방 생성 완료");
    }

    public override void OnJoinedRoom()
    {
        ShowStatus("방 참가 완료");

        // 게임 씬으로 이동
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    public override void OnLeftRoom()
    {
        ShowStatus("방에서 나왔습니다");
        SceneManager.LoadScene(0); // 로비로 돌아가기
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetActivePanel(lobbyPanel);
        ShowStatus($"방 생성 실패: {message}", true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetActivePanel(lobbyPanel);
        ShowStatus($"방 참가 실패: {message}", true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // 룸 리스트 초기화
        foreach (var entry in roomListEntries.Values)
            Destroy(entry.gameObject);
        roomListEntries.Clear();

        foreach (var roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList)
            {
                if (roomListEntries.TryGetValue(roomInfo.Name, out GameObject roomEntry))
                {
                    Destroy(roomEntry);
                    roomListEntries.Remove(roomInfo.Name);
                }
            }
            else
            {
                if (roomListEntries.TryGetValue(roomInfo.Name, out GameObject roomEntry))
                {
                    roomEntry.GetComponentInChildren<Text>().text =
                        $"{roomInfo.Name} ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})";
                }
                else
                {
                    GameObject newEntry = Instantiate(roomItemPrefab, roomGrid);
                    newEntry.GetComponentInChildren<Text>().text =
                        $"{roomInfo.Name} ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})";

                    Button joinButton = newEntry.GetComponentInChildren<Button>();
                    if (joinButton != null)
                    {
                        joinButton.onClick.AddListener(() => OnClickRoomItem(roomInfo.Name));
                    }

                    roomListEntries.Add(roomInfo.Name, newEntry);

                    // 새로운 방 번호 업데이트
                    string[] split = roomInfo.Name.Split('_');
                    if (split.Length > 1 && int.TryParse(split[1], out int num))
                    {
                        if (num >= roomNumber)
                            roomNumber = num + 1;
                    }
                }
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ShowStatus($"플레이어 입장: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ShowStatus($"플레이어 퇴장: {otherPlayer.NickName}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SetActivePanel(loginPanel);
        ShowStatus($"연결 끊김: {cause}", true);

        loginButton.interactable = true;

    }

    #endregion
    public void OnClickRoomItem(string roomName)
    {
        if (PhotonNetwork.InLobby)
        {
            JoinRoom(roomName);
        }
    }
}