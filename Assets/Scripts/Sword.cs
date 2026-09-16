using UnityEngine;
using System.Collections;
using TMPro;

public class Sword : MonoBehaviour
{
    // сслыки на внутренние компоненты
    private Rigidbody2D _Rigidbody2D;
    private Ball _Ball;
    // сслыки на внешние компоненты
    public GameObject centerRotation;     // ось вращения меча
    public TextMeshProUGUI statsTextInfo; // damage и rotSpeed
    // префабы
    public GameObject bloodEffectPrefab; // эффект крови
    public GameObject cutPrefab;         // эффект пореза при ульте

    // урон
    private float minDamage = 1.6f;
    private float maxDamage = 6.0f;
    private float curDamage;
    // скорость вращения
    private float minRotSpeed = 0.8f;
    private float maxRotSpeed = 3.0f;
    private float curRotSpeed;
    private float multiplyRotSpeed = 180f; // множитель скорости вращения
    // данные о ультимейте
    private float curUltimateCharge;
    private float chargeMaxStats = 75f; // заряд, при котором получает максмальные характеристики
    private float ultimateChargeOnHit = 5f; // заполнение шкалы ультимейта за удар
    private float ultimateHitInterval = 0.3f; // интервал между ударами
    private int ultimateHitCount = 7; // кол-во ударов
    private int ultimateDamagePerHit = 3; // урон от удара
    private float distance = 1.5f; // расстояние на котором шар возвращается после ульты
    private bool ultimateActive;

    private void Awake()
    {
        _Rigidbody2D = GetComponent<Rigidbody2D>();
        _Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _Rigidbody2D.useFullKinematicContacts = true;

        _Ball = GetComponentInParent<Ball>();

        // присвоение начального значения урона
        curDamage = minDamage;

        // присвоение начального значения скорости вращения
        curRotSpeed = minRotSpeed;
    }

    private void Start()
    {
        statsTextInfo.text = $"Damage: {curDamage:F2}\nSpin Speed: {curRotSpeed:F2}";
    }

    private void Update()
    {
        if (WorldStasis.IsFrozen && _Ball != WorldStasis.ExemptBall) return; // замирает во время ульты кинжальщика

        curUltimateCharge = _Ball.curUltimateCharge;
        
        // увеличение урона 
        curDamage = minDamage + (curUltimateCharge / chargeMaxStats) * (maxDamage - minDamage);
        if (curUltimateCharge >= chargeMaxStats) curDamage = maxDamage;
        // увеличение скорости вращения
        curRotSpeed = minRotSpeed + (curUltimateCharge / chargeMaxStats) * (maxRotSpeed - minRotSpeed);
        if (curUltimateCharge >= chargeMaxStats) curRotSpeed = maxRotSpeed;

        statsTextInfo.text = $"Damage: {curDamage:F2}\nSpin Speed: {curRotSpeed:F2}";
        // вращение меча
        centerRotation.transform.Rotate(new(0f, 0f, curRotSpeed * multiplyRotSpeed * Time.deltaTime));

        // активация ульты
        if (!ultimateActive && !WorldStasis.AnyUltimateActive && curUltimateCharge >= _Ball.maxUltimateCharge)
            StartCoroutine(UltimateRoutine());
    }

    private IEnumerator UltimateRoutine()
    {
        ultimateActive = true;
        WorldStasis.AnyUltimateActive = true;
        // отключение визуала и коллайдера
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        _Ball.SetHidden(true);

        Ball opponent = _Ball.opponent;

        for (int i = 0; i < ultimateHitCount; i++)
        {
            GameObject cut = Instantiate(cutPrefab, opponent.transform.position, Quaternion.Euler(0f, 0f, Random.Range(-90, 90)), opponent.transform);

            // Получение SpriteRenderer созданного объекта
            SpriteRenderer cutRenderer = cut.GetComponent<SpriteRenderer>();
            Color color = cutRenderer.color;

            // начальная альфа = 0
            color.a = 0f;
            cutRenderer.color = color;

            // Плавное появление эффекта
            float elapsed = 0f;
            // Длительность появления
            float fadeDuration = ultimateHitInterval * 0.95f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                if (cutRenderer != null)
                    cutRenderer.color = color;
                yield return null;
            }

            // оставшееся время до удара
            float remainingWait = ultimateHitInterval - fadeDuration;
            if (remainingWait > 0)
                yield return new WaitForSeconds(remainingWait);

            opponent.GetDamage(ultimateDamagePerHit);
            Destroy(cut);
        }

        // вычисление точки появление шара после ульты
        Vector3 directionToOwner = (_Ball.transform.position - opponent.transform.position).normalized;
        Vector3 reappearOffset = directionToOwner * distance;
        _Ball.transform.position = opponent.transform.position + reappearOffset;

        // включение визуала и коллайдера
        _Ball.SetHidden(false);
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;

        _Ball.ResetUltimateCharge();
        ultimateActive = false;
        WorldStasis.AnyUltimateActive = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball otherBall = collision.gameObject.GetComponent<Ball>();
        if (otherBall == null || otherBall.isPlayer) return;

        if (WorldStasis.IsFrozen && otherBall == WorldStasis.ExemptBall) return;

        otherBall.GetDamage((int)curDamage); // округление всегда вниз
        HitStop.Instance.Trigger(0.15f);     // хитстоп
        _Ball.AddUltimateCharge(ultimateChargeOnHit);

        ContactPoint2D contact = collision.GetContact(0);
        Quaternion bloodRotation = Quaternion.LookRotation(contact.normal);
        GameObject blood = Instantiate(bloodEffectPrefab, contact.point, bloodRotation, otherBall.transform);
        ParticleSystem.MainModule main = blood.GetComponent<ParticleSystem>().main;
        main.startColor = otherBall.transform.Find("Inside").GetComponent<SpriteRenderer>().color;
    }
}
