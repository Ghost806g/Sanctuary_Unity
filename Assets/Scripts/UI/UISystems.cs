// =========================================================================
//  SANCTUARY — UI Systems (Toast, Damage Popup, Combat Log)
//  Port de: JuiceEngine.js, toast system, floating damage
//  Efeitos visuais "juice" para a UI
// =========================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sanctuary.Core;

namespace Sanctuary.UI
{
    // ====================== TOAST SYSTEM ======================

    /// <summary>
    /// Notificações flutuantes no canto da tela.
    /// Port de: triggerToast() + toast-container no HTML
    /// </summary>
    public class ToastSystem : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private Transform toastContainer;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private int maxToasts = 5;

        private Queue<GameObject> activeToasts = new Queue<GameObject>();

        private void OnEnable()
        {
            EventBus.OnToastRequested += ShowToast;
        }

        private void OnDisable()
        {
            EventBus.OnToastRequested -= ShowToast;
        }

        public void ShowToast(string message, string type)
        {
            if (toastPrefab == null || toastContainer == null) return;

            // Limitar toasts ativos
            while (activeToasts.Count >= maxToasts)
            {
                GameObject old = activeToasts.Dequeue();
                if (old != null) Destroy(old);
            }

            GameObject toast = Instantiate(toastPrefab, toastContainer);
            TMP_Text text = toast.GetComponentInChildren<TMP_Text>();
            Image bg = toast.GetComponent<Image>();

            if (text != null) text.text = message;

            // Cores por tipo (Port das cores CSS do original)
            if (bg != null)
            {
                bg.color = type switch
                {
                    "error"     => new Color(0.94f, 0.27f, 0.27f, 0.9f), // #ef4444
                    "warning"   => new Color(0.98f, 0.75f, 0.14f, 0.9f), // #fbbf24
                    "combat"    => new Color(0.94f, 0.27f, 0.27f, 0.9f), // #ef4444
                    "legendary" => new Color(0.98f, 0.75f, 0.14f, 0.9f), // #fbbf24
                    "success"   => new Color(0.13f, 0.77f, 0.38f, 0.9f), // #22c55e
                    _           => new Color(0.15f, 0.15f, 0.20f, 0.9f)  // Dark default
                };
            }

            activeToasts.Enqueue(toast);
            StartCoroutine(FadeAndDestroy(toast));
        }

        private IEnumerator FadeAndDestroy(GameObject toast)
        {
            yield return new WaitForSeconds(displayDuration);

            CanvasGroup cg = toast.GetComponent<CanvasGroup>();
            if (cg == null) cg = toast.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            Destroy(toast);
        }
    }

    // ====================== DAMAGE POPUP ======================

    /// <summary>
    /// Números de dano flutuantes que sobem e somem.
    /// Port de: floating-log-container + efeitos de JuiceEngine.js
    /// Estilo Hades II: números que saem com impulso e desvanecem.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject popupPrefab;
        [SerializeField] private Transform popupContainer;

        [Header("Configuração")]
        [SerializeField] private float floatSpeed = 80f;
        [SerializeField] private float floatDuration = 1.2f;
        [SerializeField] private float scaleAnimDuration = 0.15f;

        private void OnEnable()
        {
            EventBus.OnDamageDealt += SpawnDamagePopup;
            EventBus.OnHealApplied += SpawnHealPopup;
        }

        private void OnDisable()
        {
            EventBus.OnDamageDealt -= SpawnDamagePopup;
            EventBus.OnHealApplied -= SpawnHealPopup;
        }

        private void SpawnDamagePopup(string target, int damage, bool isCrit)
        {
            if (popupPrefab == null || popupContainer == null) return;

            GameObject popup = Instantiate(popupPrefab, popupContainer);
            TMP_Text text = popup.GetComponentInChildren<TMP_Text>();

            if (text != null)
            {
                if (isCrit)
                {
                    text.text = $"⚡ {damage}";
                    text.color = new Color(1f, 0.84f, 0f); // Dourado para crit
                    text.fontSize = 42f;
                }
                else
                {
                    text.text = damage.ToString();
                    text.color = Color.white;
                    text.fontSize = 32f;
                }
            }

            // Posição aleatória (ligeiro offset)
            RectTransform rt = popup.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition += new Vector2(Random.Range(-30f, 30f), Random.Range(-10f, 10f));
            }

            StartCoroutine(AnimatePopup(popup, isCrit));
        }

        private void SpawnHealPopup(string target, int amount)
        {
            if (popupPrefab == null || popupContainer == null) return;

            GameObject popup = Instantiate(popupPrefab, popupContainer);
            TMP_Text text = popup.GetComponentInChildren<TMP_Text>();

            if (text != null)
            {
                text.text = $"+{amount}";
                text.color = new Color(0.13f, 0.77f, 0.38f); // Verde para cura
                text.fontSize = 32f;
            }

            StartCoroutine(AnimatePopup(popup, false));
        }

        private IEnumerator AnimatePopup(GameObject popup, bool isCrit)
        {
            RectTransform rt = popup.GetComponent<RectTransform>();
            CanvasGroup cg = popup.GetComponent<CanvasGroup>();
            if (cg == null) cg = popup.AddComponent<CanvasGroup>();

            Vector2 startPos = rt.anchoredPosition;

            // Scale punch (zoom in rápido → volta ao normal)
            if (isCrit)
            {
                float scaleElapsed = 0f;
                rt.localScale = Vector3.one * 1.8f;
                while (scaleElapsed < scaleAnimDuration)
                {
                    scaleElapsed += Time.deltaTime;
                    float t = scaleElapsed / scaleAnimDuration;
                    rt.localScale = Vector3.Lerp(Vector3.one * 1.8f, Vector3.one, t);
                    yield return null;
                }
            }

            // Float up + fade out
            float elapsed = 0f;
            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;

                rt.anchoredPosition = startPos + Vector2.up * (floatSpeed * t);
                cg.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }

            Destroy(popup);
        }
    }

    // ====================== COMBAT LOG ======================

    /// <summary>
    /// Log de combate rolante (tipo terminal).
    /// Port de: appendTerminalLog() no main_v3.js
    /// </summary>
    public class CombatLog : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Transform logContainer;
        [SerializeField] private GameObject logEntryPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Configuração")]
        [SerializeField] private int maxEntries = 100;
        [SerializeField] private Color colorSystem = new Color(0.57f, 0.64f, 0.69f);   // #94a3b8
        [SerializeField] private Color colorCombat = new Color(0.94f, 0.27f, 0.27f);    // #ef4444
        [SerializeField] private Color colorStatus = new Color(0.66f, 0.33f, 0.97f);    // #a855f7
        [SerializeField] private Color colorLoot = new Color(0.98f, 0.75f, 0.14f);      // #fbbf24

        private List<GameObject> entries = new List<GameObject>();

        private void OnEnable()
        {
            EventBus.OnCombatLogEntry += AddEntry;
        }

        private void OnDisable()
        {
            EventBus.OnCombatLogEntry -= AddEntry;
        }

        public void AddEntry(string message)
        {
            if (logContainer == null || logEntryPrefab == null) return;

            // Limitar entries
            while (entries.Count >= maxEntries)
            {
                Destroy(entries[0]);
                entries.RemoveAt(0);
            }

            GameObject entry = Instantiate(logEntryPrefab, logContainer);
            TMP_Text text = entry.GetComponentInChildren<TMP_Text>();

            if (text != null)
            {
                text.text = message;

                // Detectar tipo pela mensagem (Port da lógica de cor do original)
                if (message.Contains("⚔️") || message.Contains("💥") || message.Contains("👑"))
                    text.color = colorCombat;
                else if (message.Contains("💫") || message.Contains("🔥") || message.Contains("💚"))
                    text.color = colorStatus;
                else if (message.Contains("💰") || message.Contains("📦") || message.Contains("✨"))
                    text.color = colorLoot;
                else
                    text.color = colorSystem;
            }

            entries.Add(entry);

            // Auto-scroll para baixo
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void Clear()
        {
            foreach (var entry in entries)
                if (entry != null) Destroy(entry);
            entries.Clear();
        }
    }

    // ====================== SCREEN SHAKE (JUICE) ======================

    /// <summary>
    /// Efeito de screen shake para impacto.
    /// Port de: JuiceEngine.js screenshake
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private float defaultIntensity = 5f;
        [SerializeField] private float defaultDuration = 0.2f;

        private Vector3 originalPosition;
        private bool isShaking = false;

        private void Awake()
        {
            originalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            EventBus.OnDamageDealt += OnDamage;
            EventBus.OnEnemyStaggered += OnStagger;
        }

        private void OnDisable()
        {
            EventBus.OnDamageDealt -= OnDamage;
            EventBus.OnEnemyStaggered -= OnStagger;
        }

        private void OnDamage(string target, int damage, bool isCrit)
        {
            float intensity = isCrit ? defaultIntensity * 2f : defaultIntensity;
            Shake(intensity, defaultDuration);
        }

        private void OnStagger()
        {
            Shake(defaultIntensity * 3f, 0.4f);
        }

        public void Shake(float intensity, float duration)
        {
            if (!isShaking)
                StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            isShaking = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                transform.localPosition = originalPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                intensity *= 0.9f; // Decay
                yield return null;
            }

            transform.localPosition = originalPosition;
            isShaking = false;
        }
    }
}
