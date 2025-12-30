using System.Collections.Generic;
using UnityEngine;

namespace Spells
{
    /// <summary>
    /// Aura-style melee spell that follows the player and deals ticking damage to enemies inside.
    /// Attach to an aura prefab with a trigger Collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class AuraTickHitbox : MonoBehaviour
    {
        [Header("Damage")]
        public int damagePerTick = 5;

        [Tooltip("Seconds between damage ticks per enemy.")]
        public float tickInterval = 0.5f;

        [Header("Lifetime")]
        [Tooltip("How long the aura stays alive after spawning.")]
        public float duration = 3f;

        private readonly Dictionary<Enemy, float> _nextTickAt = new Dictionary<Enemy, float>();

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            _nextTickAt.Clear();
            if (duration > 0f) Destroy(gameObject, duration);
        }

        private void Update()
        {
            // Cleanup entries for destroyed enemies.
            if (_nextTickAt.Count == 0) return;

            // Copy keys to avoid modifying collection during iteration.
            var keys = ListPool<Enemy>.Get();
            try
            {
                foreach (var kvp in _nextTickAt) keys.Add(kvp.Key);
                for (int i = keys.Count - 1; i >= 0; i--)
                {
                    if (keys[i] == null) _nextTickAt.Remove(keys[i]);
                }
            }
            finally
            {
                ListPool<Enemy>.Release(keys);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;

            var enemy = other.GetComponent<Enemy>();
            if (enemy == null) return;

            // Allow immediate tick on entry.
            _nextTickAt[enemy] = Time.time;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;

            var enemy = other.GetComponent<Enemy>();
            if (enemy == null) return;

            if (!_nextTickAt.TryGetValue(enemy, out float nextAt))
                nextAt = Time.time;

            if (Time.time < nextAt) return;

            enemy.TakeDamage(damagePerTick);
            _nextTickAt[enemy] = Time.time + Mathf.Max(0.01f, tickInterval);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;

            var enemy = other.GetComponent<Enemy>();
            if (enemy == null) return;

            _nextTickAt.Remove(enemy);
        }

        /// <summary>
        /// Minimal list pool to avoid allocations; safe in play mode.
        /// </summary>
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

            public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>(16);

            public static void Release(List<T> list)
            {
                if (list == null) return;
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
