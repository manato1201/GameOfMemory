using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using SpeedControl;
using Unity.VisualScripting;
using DB;
using System.Collections.Generic;
using UnityEngine.UI; // Image用
using UnityEngine.SceneManagement; // シーンリロード用

using TMPro;
using Sound;
//using SpeedControl;

namespace Characters
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Player2DController : MonoBehaviour, IFilterableSpeed
    {


        [Header("移動パラメータ")]
        [SerializeField] private float movePower = 5f;
        [SerializeField] private float dashMultiplier = 2f;
        [SerializeField] private float jumpForce = 8f;
        [Header("拡縮パラメータ")]
        [SerializeField] private float scaleStep = 0.2f;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private float maxScale = 2f;
        [Header("ジャンプ回数")]
        [SerializeField] private int maxJumpCount = 2;
        // 認知回数
        //[Header("認知回数")]
        //[SerializeField] private int maxRecognitionCount = 3; // 例：3回まで
        [Header("認知回数UI（Image3つ）")]
        [SerializeField] private List<Image> recognitionImages; // Inspectorで3つ指定
        [Header("認知エリア最大ON数")]
        [SerializeField] private int maxRecognitionTargets = 3;

        [Header("説明UI（複数）")]
        [SerializeField] private List<GameObject> explanationUIs; // Inspectorで説明UI複数セット
        [Header("ゴールUI")]
        [SerializeField] private GameObject goalUI;
        [Header("鍵UI")]
        [SerializeField] private TMP_Text keyCountText; // InspectorでTextMeshProを指定

        [SerializeField] private TransitionEffectController transitionController;
        [SerializeField] private int transitionMaterialIndex = 0;


        [Header("解説UI")]
        [SerializeField] private GameObject DescribeUI;


        // 今表示しているUIのキャッシュ
        private GameObject currentExplanationUI = null;

        private int recognitionCount;
        
        //GCリスクが上がるらしい
        private List<GameObject> recognizedObjects = new List<GameObject>();
        // 今入っている認知エリア
        private List<Collider2D> enteredRecognitionAreas = new List<Collider2D>();
        

        // その他管理
        private bool isGoal = false; // ゴール状態
        private int keyCount = 0; // 鍵数

        private Rigidbody2D rb;
        private Speed speed = new Speed();
        private GameInputs inputActions;
        private Vector2 moveInput = Vector2.zero;
        private bool jumpRequested;
        private bool isSprinting;
        private bool canVerticalMove;
        private bool canScale;

        private int jumpCount;
        private SpriteRenderer spriteRenderer;
        private DB_Image imageDB;
        private Dictionary<string, Sprite[]> imageDict;

        // 画像交互切替用インデックス
        private int moveSpriteIndex = 0;
        private int jumpSpriteIndex = 0;
        private int sprintSpriteIndex = 0;

        private DB_Explanation explanationDB;
        private bool inputLocked = true; // 最初は入力ロック

        public bool UseSkillControlledSpeed { get; set; } = true;


       
        private void Awake()
        {
            explanationDB = DB_Explanation.Entity;

            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            recognitionCount = recognitionImages.Count; // 通常3
            ResetRecognitionUI();
            speed.OnValueChange += OnSpeedChanged;
            BattleSpeedController.Instance.Subscribe(speed);
            UpdateKeyCountUI();

            if (goalUI != null)
            {
                goalUI.SetActive(false);
            }

            if (DescribeUI != null)
            {
                DescribeUI.SetActive(true);
                inputLocked = true;

            }




            var uiRoot = GameObject.Find("RecognitionUI");
            if (uiRoot != null)
            {
                recognitionImages = uiRoot.GetComponentsInChildren<Image>(true).ToList();
            }


            // ScriptableObject（DB_Image）からデータ取得
            imageDB = DB_Image.Entity;
            imageDict = new Dictionary<string, Sprite[]>();
            foreach (var item in imageDB.ItemSprites)
            {
                imageDict[item.Name] = item.Sprite;
            }

            inputActions = new GameInputs();
            inputActions.Player.Move.performed += OnMove;
            inputActions.Player.Move.canceled += OnMove;
            inputActions.Player.Jump.started += OnJump;
            inputActions.Player.Sprint.performed += OnSprint;
            inputActions.Player.Sprint.canceled += OnSprint;
            inputActions.Player.Attack.started += OnAttack;
        }

        private void OnDestroy()
        {
            BattleSpeedController.Instance.Unsubscribe(speed);
            speed.OnValueChange -= OnSpeedChanged;
        }
        private void OnEnable()
        {
            inputActions.Enable();
            if (transitionController != null)
            {
                
                transitionController.PlayTransitionIn();
            
                transitionController.PlayTransitionOut(1);
            }
                
        }
        private void OnDisable()
        {
            inputActions.Disable();
        }

        // InputSystemのコールバック
        public void OnMove(InputAction.CallbackContext context)
        {
            if (inputLocked) return;

            moveInput = context.ReadValue<Vector2>();

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                // Move画像を交互に切り替え
                int spriteCount = imageDict.ContainsKey("Move") ? imageDict["Move"].Length : 1;
                SetSpriteByName("Move", moveSpriteIndex % spriteCount);
                moveSpriteIndex++;
            }
            else if (canVerticalMove && Mathf.Abs(moveInput.y) > 0.01f)
            {
                // Move画像を交互に切り替え
                int spriteCount = imageDict.ContainsKey("Climb") ? imageDict["Climb"].Length : 1;
                SetSpriteByName("Climb", moveSpriteIndex % spriteCount);
                moveSpriteIndex++;
            }
            
        }
        public void OnJump(InputAction.CallbackContext context)
        {
            if (inputLocked) return;

            if (context.started)
            {
                // ジャンプ回数制限
                if (jumpCount > 0)
                {
                   
                    jumpRequested = true;
                    jumpCount--;
                    // Jump画像を交互に切り替え
                    int spriteCount = imageDict.ContainsKey("Jump") ? imageDict["Jump"].Length : 1;
                    SetSpriteByName("Jump", jumpSpriteIndex % spriteCount);
                    jumpSpriteIndex++;
                }
            }
        }
        public void OnSprint(InputAction.CallbackContext context)
        {
            if (inputLocked) return;

            isSprinting = context.ReadValueAsButton();
            // Sprint画像を交互に切り替え
            int spriteCount = imageDict.ContainsKey("Sprint") ? imageDict["Sprint"].Length : 1;
            SetSpriteByName("Sprint", sprintSpriteIndex % spriteCount);
            sprintSpriteIndex++;
        }
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (inputLocked) return;

            // ゴール後は一切の操作無効
            if (isGoal) return; 
            if (context.started && enteredRecognitionAreas.Count > 0 && recognitionCount > -1)
            {
                // 今入っている全ての認知エリアを順にチェック（重なり複数対応）
                foreach (var area in enteredRecognitionAreas)
                {
                    int recognizedThisArea = 0;
                    foreach (Transform child in area.transform)
                    {
                        if (!child.gameObject.activeSelf && recognizedThisArea < maxRecognitionTargets && recognitionCount > -1)
                        {
                            child.gameObject.SetActive(true);
                            
                            recognizedObjects.Add(child.gameObject);
                            UpdateRecognitionUI();
                            recognizedThisArea++;
                            // 1回のAttackで1つだけ認知したい場合はbreak;
                            break;
                        }
                    }
                    if (recognizedThisArea > 0) break; // 1回で複数エリアは認知しない
                }
            }
        }

        


        private void SetSpriteByName(string name, int idx = 0)
        {
            if (imageDict == null) return;
            if (spriteRenderer == null) return;
            if (imageDict.TryGetValue(name, out var sprites))
            {
                if (sprites != null && sprites.Length > 0 && idx < sprites.Length)
                {
                    spriteRenderer.sprite = sprites[idx];
                }
            }
        }

        private void FixedUpdate()
        {
            // 説明UIが表示中 && 何か入力されたらUI非表示＆ロック解除
            if (inputLocked && DescribeUI != null && DescribeUI.activeSelf)
            {
               
                if (
                   (Keyboard.current != null && Keyboard.current.anyKey.isPressed) ||
                   (Mouse.current != null && Mouse.current.leftButton.isPressed)
                )
                {
                    SoundManager.Instance.PlaySE("OK");
                    transitionController.PlayTransitionOut(transitionMaterialIndex, () =>
                    {
                        DescribeUI.SetActive(false);
                        inputLocked = false;
                        transitionController.PlayTransitionIn();
                    });
                   
                }
                return; // 入力ロック中はこれ以下のUpdate処理を全てスキップ
            }

            float appliedPower = movePower * (isSprinting ? dashMultiplier : 1f) * speed.CurrentSpeed;
            float vx = moveInput.x * appliedPower;
            float vy = rb.linearVelocity.y;

            if (canVerticalMove)
            {
                vy = moveInput.y * appliedPower;
            }
            else if (canScale)
            {
                if (moveInput.y > 0.2f)
                    ScaleCharacter(true);
                else if (moveInput.y < -0.2f)
                    ScaleCharacter(false);
            }

            rb.linearVelocity = new Vector2(vx, vy);

            // ジャンプ処理
            if (jumpRequested && IsGrounded())
            {
                SoundManager.Instance.PlaySE("Jump");
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpRequested = false;
            }
        }

        private void ScaleCharacter(bool enlarge)
        {
            SoundManager.Instance.PlaySE("Scale");
            float current = transform.localScale.x;
            float target = enlarge
                ? Mathf.Min(current + scaleStep, maxScale)
                : Mathf.Max(current - scaleStep, minScale);
            transform.localScale = new Vector3(target, target, 1f);
        }

        private bool IsGrounded()
        {
            // 地面との距離判定(お好みで調整)
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f);
            return hit.collider != null;
        }

        

        // UIの色を更新（認知回数が減るたびに左から黒く）
        private void UpdateRecognitionUI()
        {
            int idx = recognitionImages.Count - recognitionCount;
            if (idx >= 0 && idx < recognitionImages.Count)
            {
                if (recognitionImages[idx] != null)
                {
                    recognitionImages[idx].color = Color.black;
                }
                
            }
            recognitionCount--;

            // 認知回数0でリセット
            if (recognitionCount <= -1)
            {
                //Invoke(nameof(ResetRecognitionsAndReload), 0.5f);
                ResetRecognitionsAndReload();
            }
        }

        private void ResetRecognitionUI()
        {
            foreach (var img in recognitionImages)
            {
                if (img != null) img.color = Color.white;
            }
        }


        



        private void ResetRecognitionsAndReload()
        {
            SoundManager.Instance.PlaySE("Dead");
            // 全てオフに戻す
            foreach (var obj in recognizedObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
            recognizedObjects.Clear();
            recognitionCount = recognitionImages.Count;
            ResetRecognitionUI();
            


            transitionController.PlayTransitionOut(transitionMaterialIndex, () =>
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    // 新シーンで PlayTransitionOut() を呼ぶ
                });
            
        }
        private void UpdateKeyCountUI()
        {
            if (keyCountText != null)
            {
                keyCountText.text = keyCount.ToString();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {

            if (other.CompareTag("Recognition"))
            {
                if (!enteredRecognitionAreas.Contains(other))
                {
                    SoundManager.Instance.PlaySE("Recognition");
                    enteredRecognitionAreas.Add(other);
                }
            }
            if (other.CompareTag("Incorrect"))
            {
                SoundManager.Instance.PlaySE("Incorrect");

            }

            if (other.CompareTag("VerticalArea"))
            {
                canVerticalMove = true;
                Debug.Log(canVerticalMove);

            }
                
            if (other.CompareTag("ScaleArea"))
            {
                canScale = true;
                Debug.Log(canScale);
            }

            // --- Explanation ---
            if (other.CompareTag("Explanation"))
            {
                SoundManager.Instance.PlaySE("Explanation");
                string areaPath = other.transform.GetHierarchyPath();
                Debug.Log($"areaPath: {areaPath}");
                var pair = explanationDB.ExplanationObjects.FirstOrDefault(x => x.AreaName == areaPath);
                if (pair != null)
                {
                    var uiObj = FindInactiveUI(pair.UIName);
                    if (uiObj != null)
                    {
                        
                        uiObj.SetActive(true);
                        currentExplanationUI = uiObj;
                    }
                    else
                    {
                        Debug.Log("NoUI: " + pair.UIName);
                    }
                }
            }

            // --- Dead ---
            if (other.CompareTag("Dead"))
            {
                
                ResetRecognitionsAndReload();
            }

            // --- Recovery ---
            if (other.CompareTag("Recovery"))
            {
                SoundManager.Instance.PlaySE("Recovery");
                recognitionCount = recognitionImages.Count;
                ResetRecognitionUI();
                Destroy(other.gameObject);
            }

            // --- Goal ---
            if (other.CompareTag("Goal"))
            {
                if (goalUI != null)
                {
                    SoundManager.Instance.PlaySE("correct");
                    goalUI.SetActive(true);
                    isGoal = true;
                }
            }

            // --- Keyタグ ---
            if (other.CompareTag("Key"))
            {
                SoundManager.Instance.PlaySE("Key");
                int oldCount = keyCount;
                keyCount++;
                UpdateKeyCountUI();
                Destroy(other.gameObject); // 触れたオブジェクトを破壊
            }

            // --- Obstacleタグ ---
            if (other.CompareTag("Obstacle"))
            {
                int oldCount = keyCount;
                if (keyCount > 0)
                {
                    SoundManager.Instance.PlaySE("Obstacle");
                    keyCount--;
                    
                    UpdateKeyCountUI();
                    Destroy(other.gameObject); // 触れたオブジェクトを破壊
                }
                // 0のときは何もしない（破壊もなし）
            }



        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("VerticalArea"))
            {
                canVerticalMove = false;
                Debug.Log(canVerticalMove);
            }
                
            if (other.CompareTag("ScaleArea"))
            {
                canScale = false;
                Debug.Log(canScale);
            }

            if (other.CompareTag("Recognition"))
            {
                enteredRecognitionAreas.Remove(other);
            }
            // --- Explanation ---
            if (other.CompareTag("Explanation"))
            {
                if (other.CompareTag("Explanation"))
                {
                    // 今表示中のUIがあれば非表示に
                    if (currentExplanationUI != null)
                    {
                        currentExplanationUI.SetActive(false);
                        currentExplanationUI = null;
                    }
                }
            }



        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 地面タグ（またはLayerなどで地面判定）でジャンプ回数リセット
            if (collision.collider.CompareTag("Ground"))
            {
                jumpCount = maxJumpCount;
            }
        }
        // 必要なら鍵数取得用プロパティ
        public int GetKeyCount() => keyCount;

        private void OnSpeedChanged(float newSpeed)
        {
            // 速度変化時に即時反映したいロジックがあればここに
        }


        GameObject FindInactiveUI(string fullPath)
        {
            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.transform.GetHierarchyPath() == fullPath)
                    return obj;
            }
            return null;
        }

    }
}

public static class TransformExtensions
{
    public static string GetHierarchyPath(this Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}




[RequireComponent(typeof(Rigidbody2D))]
public class Enemy2DController : MonoBehaviour, IFilterableSpeed
{
    [SerializeField] private float movePower = 3f;
    [SerializeField] private Transform target; // 追いかけたい対象（プレイヤーなど）

    private Rigidbody2D rb;
    private Speed speed = new Speed();

    public bool UseSkillControlledSpeed { get; set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        speed.OnValueChange += OnSpeedChanged;
        BattleSpeedController.Instance.Subscribe(speed);
    }

    private void OnDestroy()
    {
        BattleSpeedController.Instance.Unsubscribe(speed);
        speed.OnValueChange -= OnSpeedChanged;
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = direction * movePower * speed.CurrentSpeed;
    }

    private void OnSpeedChanged(float newSpeed)
    {
        // 速度変化時に即時反映したいロジックがあればここに
    }
}
