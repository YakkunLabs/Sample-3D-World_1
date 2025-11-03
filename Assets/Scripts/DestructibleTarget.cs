using UnityEngine;
using UnityEngine.UI; // For the Slider
using TMPro; // For the Text

public class DestructibleTarget : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public Slider healthSlider;
    public TMP_Text healthText;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        UpdateHealthUI();

        if (currentHealth == 0)
        {
            // Optional: Destroy the target
            // Destroy(gameObject);
        }
    }

    void UpdateHealthUI()
    {
        float healthPercent = currentHealth / maxHealth;
        healthSlider.value = healthPercent;
        healthText.text = currentHealth.ToString("F0") + " / " + maxHealth;
    }
}