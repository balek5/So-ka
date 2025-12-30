using UnityEngine;
using Spells;

public class PlayerSpells : MonoBehaviour
{
    public Spell[] equippedSpells = new Spell[4];
    public Transform castPoint;
    public float autoCastRange = 15f;

    private float[] lastCastTime = new float[4];
    private Rigidbody playerRb;
    private PlayerProgression playerStats;
    private PlayerModifiers modifiers;

    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerStats = GetComponent<PlayerProgression>();
        modifiers = GetComponent<PlayerModifiers>();
        if (modifiers == null) modifiers = gameObject.AddComponent<PlayerModifiers>();
    }

    void Update()
    {
        Enemy nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null) return;

        for (int i = 0; i < equippedSpells.Length; i++)
        {
            Spell spell = equippedSpells[i];
            if (spell == null) continue;

            float baseAttackSpeed = playerStats != null ? playerStats.attackSpeed : 1f;
            float effectiveAttackSpeed = baseAttackSpeed * (modifiers != null ? modifiers.attackSpeedMultiplier : 1f);
            effectiveAttackSpeed = Mathf.Max(0.01f, effectiveAttackSpeed);

            float cooldownMult = (playerStats != null ? playerStats.spellCooldownMultiplier : 1f) * (modifiers != null ? modifiers.cooldownMultiplier : 1f);
            cooldownMult = Mathf.Max(0.01f, cooldownMult);

            float effectiveCooldown = (spell.cooldown / effectiveAttackSpeed) * cooldownMult;

            if (Time.time >= lastCastTime[i] + effectiveCooldown)
            {
                CastSpell(i, nearestEnemy.transform);
                lastCastTime[i] = Time.time;
            }
        }
    }

    void CastSpell(int index, Transform target)
    {
        Spell spell = equippedSpells[index];
        if (spell == null || spell.spellPrefab == null || castPoint == null) return;

        float baseDmgMult = playerStats != null ? playerStats.spellDamageMultiplier : 1f;
        float overallDmgMult = modifiers != null ? modifiers.damageMultiplier : 1f;
        float dmgMult = baseDmgMult * overallDmgMult;
        int scaledDamage = Mathf.RoundToInt(spell.baseDamage * dmgMult);

        float sizeMult = (modifiers != null ? modifiers.sizeMultiplier : 1f) * spell.sizeMultiplier;
        sizeMult = Mathf.Max(0.01f, sizeMult);

        int totalProjectiles = spell.projectileCount + (modifiers != null ? modifiers.bonusProjectiles : 0);
        totalProjectiles = Mathf.Max(1, totalProjectiles);

        for (int p = 0; p < totalProjectiles; p++)
        {
            // NEW: some spells should spawn directly on the target (katana VFX on enemy)
            Vector3 spawnPos = castPoint.position;
            Quaternion spawnRot = castPoint.rotation;

            if (spell.spawnOnTarget && target != null)
            {
                spawnPos = target.position;
                spawnRot = Quaternion.LookRotation((target.position - transform.position).normalized);
            }

            GameObject obj = Instantiate(spell.spellPrefab, spawnPos, spawnRot);
            obj.SetActive(true);

            // Scale size (runtime only)
            obj.transform.localScale *= sizeMult;

            if (!spell.isMelee)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (target.position - spawnPos).normalized;

                    // Spread for multi-shot
                    if (totalProjectiles > 1)
                    {
                        float angle = (p - (totalProjectiles - 1) / 2f) * spell.projectileSpreadAngle;
                        dir = Quaternion.Euler(0, angle, 0) * dir;
                    }

                    rb.linearVelocity = dir * spell.speed + (playerRb != null ? playerRb.linearVelocity : Vector3.zero);
                }

                // Homing
                if (spell.homing)
                {
                    HomingProjectile homingProj = obj.GetComponent<HomingProjectile>();
                    if (homingProj != null)
                    {
                        homingProj.target = target;
                        homingProj.damage = scaledDamage;
                        homingProj.speed = spell.speed;
                    }
                    else
                    {
                        // Fallback to non-homing damage component if prefab doesn't have homing script
                        SpellProjectile proj = obj.GetComponent<SpellProjectile>();
                        if (proj != null) proj.damage = scaledDamage;
                    }
                }
                else
                {
                    SpellProjectile proj = obj.GetComponent<SpellProjectile>();
                    if (proj != null) proj.damage = scaledDamage;
                }
            }
            else
            {
                // Prefer aura ticking hitbox if present
                AuraTickHitbox aura = obj.GetComponent<AuraTickHitbox>();
                if (aura != null)
                {
                    aura.damagePerTick = scaledDamage;
                    // Use spell cooldown as a reasonable default tick interval unless overridden in prefab
                    aura.tickInterval = Mathf.Max(0.05f, spell.cooldown);

                    // If it's an aura style spell, follow the player
                    obj.transform.SetParent(transform);
                }
                else
                {
                    SpellHitbox hitbox = obj.GetComponent<SpellHitbox>();
                    if (hitbox != null) hitbox.damage = scaledDamage;

                    Vector3 lookDir = target.position - obj.transform.position;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                        obj.transform.rotation = Quaternion.LookRotation(lookDir);

                    // If this melee spell is meant to be attached (old behavior)
                    if (!spell.spawnOnTarget)
                        obj.transform.SetParent(transform);

                    ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particles) ps.Play();
                }
            }

            // Lifetime: for spawn-on-target katana VFX you usually want it short
            Destroy(obj, spell.isMelee ? (spell.spawnOnTarget ? 0.75f : 0.5f) : 3f);
        }
    }

    Enemy FindNearestEnemy()
    {
        Enemy[] enemies = GameObject.FindObjectsOfType<Enemy>();
        Enemy closest = null;
        float minDist = autoCastRange;

        foreach (Enemy e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist <= minDist)
            {
                closest = e;
                minDist = dist;
            }
        }

        return closest;
    }

    public void EquipSpell(Spell newSpell)
    {
        for (int i = 0; i < equippedSpells.Length; i++)
        {
            if (equippedSpells[i] == null)
            {
                equippedSpells[i] = newSpell;
                return;
            }
        }
        equippedSpells[0] = newSpell;
    }
}
