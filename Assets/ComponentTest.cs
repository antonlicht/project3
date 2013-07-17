using UnityEngine;
using System.Collections;

public class ComponentTest : MonoBehaviour 
{
	private Health health;
	private Speed speed;
	private Damage damage;

	private float hitRateCounter;

	public float testMaxHealth;
	public float testHealth;

	public float testDefaultSpeed;
	public float testMaxStamina;
	public float testStamina;
	public float testStaminaRegenaration;

	public float testDefaultDamage;
	public float testIncDamage;
	public float testHitSpeed;
	public float testIncHitSpeed;
	public float testDamageMultiplier;
	public float testHitSpeedMultiplier;

	public bool attack;

	// Use this for initialization
	void Start () 
	{
		health = gameObject.GetComponent<Health>();
		speed = gameObject.GetComponent<Speed>();
		damage = gameObject.GetComponent<Damage>();

		health.alive = true;
		health.SetMaxHealth(testMaxHealth, true);
		health.SetHealthToValue(testHealth);

		speed.SetDefaultSpeed(testDefaultSpeed);
		speed.SetMaxStamina(testMaxStamina);
		speed.SetStamina(testStamina);
		speed.SetStaminaRegenaration(testStaminaRegenaration);

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
		testStamina = speed.Stamina;

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
