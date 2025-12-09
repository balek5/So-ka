using UnityEngine;

public class PlayerSpells : MonoBehaviour
{
    public Spell[] equippedSpells = new Spell[4];
    public Transform castPoint;
    public float autoCastRange = 15f;

    private float[] lastCastTime = new float[4];
    private Rigidbody playerRb;
    private PlayerProgression playerStats;

    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerStats = GetComponent<PlayerProgression>();
    }

    void Update()
    {
        Enemy nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null) return;

        for (int i = 0; i < equippedSpells.Length; i++)
        {
            Spell spell = equippedSpells[i];
            if (spell == null) continue;

            float effectiveCooldown = spell.cooldown / playerStats.attackSpeed;
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
        if (spell.spellPrefab == null || castPoint == null) return;

        int scaledDamage = Mathf.RoundToInt(spell.baseDamage * playerStats.spellDamageMultiplier);

        for (int p = 0; p < spell.projectileCount; p++)
        {
            GameObject obj = Instantiate(spell.spellPrefab, castPoint.position, castPoint.rotation);
            obj.SetActive(true);

            // Scale size
            obj.transform.localScale *= spell.sizeMultiplier;

            if (!spell.isMelee)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (target.position - castPoint.position).normalized;

                    // Spread for multi-shot
                    if (spell.projectileCount > 1)
                    {
                        float angle = (p - (spell.projectileCount - 1) / 2f) * spell.projectileSpreadAngle;
                        dir = Quaternion.Euler(0, angle, 0) * dir;
                    }

                    rb.linearVelocity = dir * spell.speed + playerRb.linearVelocity;
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
                }
                else
                {
                    SpellProjectile proj = obj.GetComponent<SpellProjectile>();
                    if (proj != null) proj.damage = scaledDamage;
                }
            }
            else
            {
                SpellHitbox hitbox = obj.GetComponent<SpellHitbox>();
                if (hitbox != null) hitbox.damage = scaledDamage;

                Vector3 lookDir = target.position - obj.transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    obj.transform.rotation = Quaternion.LookRotation(lookDir);

                obj.transform.SetParent(transform);

                ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles) ps.Play();
            }

            Destroy(obj, spell.isMelee ? 0.5f : 3f);
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
