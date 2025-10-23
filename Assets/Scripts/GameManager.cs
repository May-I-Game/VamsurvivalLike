using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager instance;
    [Header("# Game Control")]
    public bool isGame;
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 2 * 10f;

    [Header("# Player Info")]
    public int playerId;
    public float health;
    public float maxHealth = 100;
    public int level;
    public int kill;
    public int exp;
    public int[] nextExp = { 10, 30, 60, 100, 150, 210, 280, 360, 450, 600 };

    [Header("# Game Object")]
    public CinemachineCamera cineCamera;
    public Button startButton;
    public GameObject hud;
    public PoolManager pool;
    public PlayerController player;
    public GameObject playerPrefab;
    public LevelUp uiLevelUp;
    public Result uiResult;
    public GameObject enemyCleaner;

    private void Awake()
    {
        instance = this;
    }

    private void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            Vector3 randomPos = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);
            GameObject player_object = PhotonNetwork.Instantiate(playerPrefab.name, randomPos, Quaternion.identity);

            PhotonView pv = player_object.GetComponent<PhotonView>();
            pv.RPC("InitPlayer", RpcTarget.AllBuffered, playerId);

            player = player_object.GetComponent<PlayerController>();
            cineCamera.Follow = player.transform;
        }
    }

    public void GameReady(int id)
    {
        playerId = id;
        SpawnPlayer();

        // 플레이어가 준비되었음을 알림
        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
        playerProps.Add("isReady", true);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 바뀐 속성에 "isReady"가 포함되어 있는지 확인
        if (changedProps.ContainsKey("isReady"))
        {
            CheckAllPlayersReady();
        }
    }

    private void CheckAllPlayersReady()
    {
        bool allReady = true;
        // 1. 방에 있는 모든 플레이어를 순회
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            // 2. CustomProperties에 "isReady" 키가 없거나, 값이 false라면
            if (!p.CustomProperties.ContainsKey("isReady") || (bool)p.CustomProperties["isReady"] == false)
            {
                // 3. 아직 준비 안 된 사람이 있으므로 함수 종료
                allReady = false;
                break;
            }
        }

        // 시작 버튼 활성화/비활성화
        startButton.gameObject.SetActive(allReady);
    }

    public void OnStartGameButtonClicked()
    {
        // 게임 시작했으니 버튼끄기
        startButton.gameObject.SetActive(false);

        // "StartGame_RPC"라는 이름의 함수를 "모두에게"(All) 전송
        GetComponent<PhotonView>().RPC("StartGame_RPC", RpcTarget.All);
    }

    [PunRPC]
    public void StartGame_RPC()
    {
        // (중요!) 이제 모든 클라이언트가 *동시에* // 기존에 만들어두신 GameStart() 함수를 호출합니다.
        GameStart();
    }

    public void GameStart()
    {
        isGame = true;
        health = maxHealth;

        uiLevelUp.Select(playerId % 2);
        hud.SetActive(true);
        Resume();

        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    public void GameOver()
    {
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false;
        isGame = false;
        yield return new WaitForSeconds(0.5f);

        uiResult.gameObject.SetActive(true);
        uiResult.Lose();
        Stop();

        AudioManager.instance.PlayBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
    }

    public void GameVictory()
    {
        StartCoroutine(GameGameVictoryRoutine());
    }

    IEnumerator GameGameVictoryRoutine()
    {
        isLive = false;
        isGame = false;
        enemyCleaner.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        uiResult.gameObject.SetActive(true);
        uiResult.Win();
        Stop();

        AudioManager.instance.PlayBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Win);
    }

    public void GameRetry()
    {
        SceneManager.LoadScene(0);
    }

    private void Update()
    {
        if (!isLive || !isGame)
            return;

        gameTime += Time.deltaTime;

        if (gameTime > maxGameTime)
        {
            gameTime = maxGameTime;
            GameVictory();
        }
    }

    public void GetExp()
    {
        if (!isLive)
            return;

        exp++;

        if (exp == nextExp[Mathf.Min(level, nextExp.Length - 1)])
        {
            level++;
            exp = 0;
            uiLevelUp.Show();
        }
    }

    public void Stop()
    {
        isLive = false;
        Time.timeScale = 0;
    }

    public void Resume()
    {
        isLive = true;
        Time.timeScale = 1;
    }
}
