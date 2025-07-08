using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SpeedControl;
using Unity.VisualScripting;
using DB;
using System.Collections.Generic;
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

        private Rigidbody2D rb;
        private Speed speed = new Speed();
        private GameInputs inputActions;
        private Vector2 moveInput = Vector2.zero;
        private bool jumpRequested;
        private bool isSprinting;
        private bool canVerticalMove;
        private bool canScale;

        private SpriteRenderer spriteRenderer;
        // 画像切り替え用
        private DB_Image imageDB;
        private Dictionary<string, Sprite[]> imageDict;

        public bool UseSkillControlledSpeed { get; set; } = true;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            speed.OnValueChange += OnSpeedChanged;
            BattleSpeedController.Instance.Subscribe(speed);

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
        }
        private void OnDisable()
        {
            inputActions.Disable();
        }

        // InputSystemのコールバック
        public void OnMove(InputAction.CallbackContext context)
        {
            Debug.Log("Move");
            moveInput = context.ReadValue<Vector2>();
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                SetSpriteByName("Move", 1); // 画像DB内「Move」のSprite配列の0番をセット
                SetSpriteByName("Move", 2);
            }
            else if (canVerticalMove && Mathf.Abs(moveInput.y) > 0.01f)
            {
                SetSpriteByName("Climb", 0);
                SetSpriteByName("Climb", 1);
            }
            else
            {
                SetSpriteByName("Move", 0);
            }
        }
        public void OnJump(InputAction.CallbackContext context)
        {
            Debug.Log("Jump");
            if (context.started)
            {
                jumpRequested = true;
                SetSpriteByName("Jump", 0);
            }
                
        }
        public void OnSprint(InputAction.CallbackContext context)
        {
            Debug.Log("Sprint");
            isSprinting = context.ReadValueAsButton();
            SetSpriteByName("Move", 1); // 画像DB内「Move」のSprite配列の0番をセット
            SetSpriteByName("Move", 2);
        }
        public void OnAttack(InputAction.CallbackContext context)
        {
            Debug.Log("Attack");
            if (context.started)
            {
                Attack();
            }
                
        }


        private void SetSpriteByName(string name, int idx = 0)
        {
            if (imageDict == null)
            {
                Debug.LogWarning("imageDictがnullです！");
                return;
            }
            if (spriteRenderer == null)
            {
                Debug.LogWarning("spriteRendererがnullです！");
                return;
            }
            if (imageDict.TryGetValue(name, out var sprites))
            {
                if (sprites != null && sprites.Length > idx)
                {
                    spriteRenderer.sprite = sprites[idx];
                }
            }
            else
            {
                Debug.LogWarning($"Sprite名 '{name}' がDB_Imageに登録されていません");
            }
        }

        private void FixedUpdate()
        {
            float appliedPower = movePower * (isSprinting ? dashMultiplier : 1f) * speed.CurrentSpeed;

            float vx = moveInput.x * appliedPower;
            float vy = rb.linearVelocity.y;

            // 優先度：縦移動＞拡縮＞通常
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
                // Y方向移動は変えない
            }

            rb.linearVelocity = new Vector2(vx, vy);

            // ジャンプ処理
            if (jumpRequested && IsGrounded())
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpRequested = false;
            }
        }

        private void ScaleCharacter(bool enlarge)
        {
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

        private void Attack()
        {
            Debug.Log("Attack!");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("VerticalArea"))
                canVerticalMove = true;
            if (other.CompareTag("ScaleArea"))
                canScale = true;
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("VerticalArea"))
                canVerticalMove = false;
            if (other.CompareTag("ScaleArea"))
                canScale = false;
        }

        private void OnSpeedChanged(float newSpeed)
        {
            // 速度変化時に即時反映したいロジックがあればここに
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
}