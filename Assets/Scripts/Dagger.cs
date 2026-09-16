using UnityEngine;

public class Dagger : MonoBehaviour
{
    // сслыки на внутренние компоненты
    private Rigidbody2D _Rigidbody2D;
    // сслыки на внешние компоненты
    public Ball ownerBall;        // ссылка на шар владелец
    public DaggerSpawner spawner; // ссылка на объект родитель спавнер кинжалов
    // префабы
    public GameObject bloodEffectPrefab; // префаб эффекта ранения

    // данные о уроне кинжала
    private int damage = 3;
    // данные о направлении движения
    private Vector3 direction; // направление движения
    // данные о скорости и ускорении
    private float startSpeed   = 13f; // начальная скорость кижала
    private float maxSpeed     = 88f; // максимальная скорость
    private float acceleration = 12f; // ускорение
    private float speed;              // текущая скорость
    // данные о фазе движения
    private enum Phase { Moving, Stopped, Accelerating } // все фазы движения кинжала
    private Phase phase;                                 // текущая фаза
    // данные о времени затраченном на каждую фазу
    private float moveDuration = 0.05f; // сколько секунд лететь до остановки
    private float stopDuration = 0.5f;  // сколько секунд стоять
    private float phaseTimer   = 0f;    // счётчик текущей фазы (в секундах)
    // данные об ультимейте
    private float ultimateChargeOnHit = 5f;
    private bool isUltimateDagger;
    private bool isDormant; // ждёт конца заморозки, ещё не летит


    private void Awake()
    {
        _Rigidbody2D = GetComponent<Rigidbody2D>();
        _Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _Rigidbody2D.useFullKinematicContacts = true;

        // присвоение начальной скорости
        speed = startSpeed;
        // присвоение начальной фазы
        phase = Phase.Moving;
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (isDormant) return;
        if (WorldStasis.IsFrozen) return; // обычные кинжалы замирают во время ульты

        phaseTimer += Time.deltaTime;

        // изменение фазы движения кинжала
        switch (phase)
        {
            case Phase.Moving:
                transform.Translate(direction * speed * Time.deltaTime, Space.World);
                if (phaseTimer >= moveDuration)
                {
                    phaseTimer = 0f;
                    phase = Phase.Stopped;
                }
                break;

            case Phase.Stopped:
                if (phaseTimer >= stopDuration)
                {
                    phaseTimer = 0f;
                    phase = Phase.Accelerating;
                }
                break;

            case Phase.Accelerating:
                speed = Mathf.Min(speed + acceleration * Time.deltaTime, maxSpeed);
                transform.Translate(direction * speed * Time.deltaTime, Space.World);
                break;
        }

        // вылет за границы поля
        if (transform.position.x <= -3f || transform.position.x >= 3f ||
            transform.position.y <= -3f || transform.position.y >= 3f)
            Destroy(gameObject);

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // вычисление шара оппонента
        Ball otherBall = collision.gameObject.GetComponent<Ball>();
        if (otherBall == null || !otherBall.isPlayer) return;

        // нанесение урона оппоненту
        otherBall.GetDamage(damage);
        // увеличение скорости спана кинжалов и зарядка ульты
        if (!isUltimateDagger)
        {
            ownerBall.AddUltimateCharge(ultimateChargeOnHit); // зарядка ульты
            spawner.IncreaseSpawnSpeed(); // уменьшение кулдауна спавна
        }

        // появление в месте контакта
        ContactPoint2D contact = collision.GetContact(0);
        Quaternion bloodRotation = Quaternion.LookRotation(contact.normal);
        GameObject blood = Instantiate(bloodEffectPrefab, contact.point, bloodRotation, otherBall.transform);
        ParticleSystem.MainModule main = blood.GetComponent<ParticleSystem>().main;
        main.startColor = otherBall.transform.Find("Inside").GetComponent<SpriteRenderer>().color;

        // удаление кинжала
        Destroy(gameObject);
    }

    public void ChangePhaseForUlt() => phase = Phase.Accelerating;
    public void ChangeDirection(Vector3 newDirection) => direction = newDirection;
    public void ChangeIsDormant(bool newIsDormant) => isDormant = newIsDormant;
    public void ChangeIsUltimateDagger(bool newIsUltimateDagger) => isUltimateDagger = newIsUltimateDagger;
}
