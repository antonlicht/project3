using UnityEngine;
using System.Collections;

public class ComponentTest : MonoBehaviour 
{
	private Health health;
	private Speed speed;
	private Damage damage;

	private float hitRateCounter;

	public float testMaxHealth;
	public float testMinHealth;
	public float testHealth;
	public float testMaxHealthMultiplier;

	public float testDefaultSpeed;
	public float testMinStamina;
	public float testMaxStamina;
	public float testStamina;
	public float testStaminaRegenaration;
	public float testStaminaDecay;

	public float testDefaultDamage;
	public float testIncDamage;
	public float testHitSpeed;
	public float testIncHitSpeed;
	public float testDamageMultiplier;
	public float testHitSpeedMultiplier;

	public bool attack;
	public bool sprinting;

	// Use this for initialization
	void Start () 
	{
		health = gameObject.GetComponent<Health>();
		speed = gameObject.GetComponent<Speed>();
		damage = gameObject.GetComponent<Damage>();

		health.alive = true;
		health.SetMaxHealthMultiplier(testMaxHealthMultiplier);
		health.SetMinHealth(testMinHealth);	
		health.SetMaxHealth(testMaxHealth, true);

		speed.SetDefaultSpeed(testDefaultSpeed);
		speed.SetMaxStamina(testMaxStamina);
		speed.SetMinStamina(testMinStamina);
		speed.SetStamina(testStamina);
		speed.SetStaminaRegenaration(testStaminaRegenaration);
		speed.SetStaminaDecay(testStaminaDecay);

		damage.SetDefaultDamage(testDefaultDamage);
		damage.SetIncDamage(testIncDamage);
		damage.SetHitSpeed(testHitSpeed);
		damage.SetIncHitSpeed(testIncHitSpeed);
		damage.SetDamageMultiplier(testDamageMultiplier);
		damage.SetHitSpeedMultiplier(testHitSpeedMultiplier);

		attack = false;
	}
	
	// Update is called once per frame
	void Update () 
	{		
		testHealth = health.HealthPoints;
		testMaxHealth = health.MaxHealth;
		testMinHealth = health.MinHealth;
		testStamina = speed.Stamina;

		testDefaultDamage = damage.CurrentDamage;
		testHitSpeed = damage.HitSpeed;

		speed.IsSprinting = sprinting;
		sprinting = speed.IsSprinting;

		if (attack)
		{
			hitRateCounter += Time.deltaTime;
			if (hitRateCounter >= 1 / testHitSpeed)
			{
				health.DecHealth(testDefaultDamage);
				hitRateCounter = 0;
			}          
		}
	}
}
