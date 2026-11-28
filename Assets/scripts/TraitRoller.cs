using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TraitRoller : MonoBehaviour
{
    public static List<Trait> ActiveTraits = new List<Trait>();
    public class Trait
    {
        public string Name { get; private set; }
        public int DamageBonus { get; private set; }
        public int DefenseBonus { get; private set; }
        public int HealthBonus { get; private set; }
        public int SpeedBonus { get; private set; }
        public int DashDistanceBonus { get; private set; }
        public int LongAttackBonus { get; private set; }
        public int SwingBonus { get; private set; }

        public Trait(string name, int damageBonus = 0, int defenseBonus = 0, int healthBonus = 0, int speedBonus = 0, int dashDistanceBonus = 0, int longAttackBonus = 0, int swingBonus = 0)
        {
            Name = name;
            DamageBonus = damageBonus;
            DefenseBonus = defenseBonus;
            HealthBonus = healthBonus;
            SpeedBonus = speedBonus;
            DashDistanceBonus = dashDistanceBonus;
            LongAttackBonus = longAttackBonus;
            SwingBonus = swingBonus;
        }
    }

    [Header("UI Reference")]
    public TextMeshProUGUI[] slotTexts; // Přetáhni sem 3 TextMeshPro texty pro sloty
    public Button spinButton;           // Tlačítko Spin

    [Header("Nastavení animace točení")]
    public float spinDuration = 2.0f;   // Jak dlouho se bude celkem točit (v sekundách)

    private Dictionary<string, Trait> allTraits = new Dictionary<string, Trait>();
    public List<Trait> RolledTraits { get; private set; } = new List<Trait>();

    private bool hasSpun = false;       // Hlídá, zda už hráč svůj jediný spin použil

    public Button Goplay { get; private set; }
    private void Awake()
    {
        InitializeTraits();
    }

    private void InitializeTraits()
    {
        allTraits.Clear();

        allTraits["Tough"] = new Trait(name: "Tough", defenseBonus: 4, healthBonus: 20);
        allTraits["Agile"] = new Trait(name: "Agile", speedBonus: 4, dashDistanceBonus: 10);
        allTraits["Intelligent"] = new Trait(name: "Intelligent", longAttackBonus: 7);
        allTraits["Berserker"] = new Trait(name: "Berserker", damageBonus: 6, defenseBonus: -2);
        allTraits["Resilient"] = new Trait(name: "Resilient", healthBonus: 15, defenseBonus: 3);
        allTraits["Chosen One"] = new Trait(name: "Chosen One", damageBonus: 10, healthBonus: 10, speedBonus: 5, dashDistanceBonus: 15, swingBonus: 5, longAttackBonus: 7);
        allTraits["Spider"] = new Trait(name: "Spider", swingBonus: 10);
        allTraits["Ox"] = new Trait(name: "Ox", healthBonus: 8, defenseBonus: 25);
        allTraits["True Demon"] = new Trait(name: "True Demon", damageBonus: 15, defenseBonus: 5, healthBonus: 30, speedBonus: 10, dashDistanceBonus: 20, swingBonus: 10, longAttackBonus: 10);
        allTraits["Lucker"] = new Trait(name: "Lucker", speedBonus: 7, dashDistanceBonus: 7, swingBonus: 7, longAttackBonus: 7, healthBonus: 7, defenseBonus: 7, damageBonus: 7); //everything 7 
    }

    public List<Trait> RollTraits(int count)
    {
        RolledTraits.Clear();
        List<Trait> availableTraits = new List<Trait>(allTraits.Values);
        int rollCount = Mathf.Min(count, availableTraits.Count);

        for (int i = 0; i < rollCount; i++)
        {
            int randomIndex = Random.Range(0, availableTraits.Count);
            var trait = availableTraits[randomIndex];
            RolledTraits.Add(trait);
            availableTraits.RemoveAt(randomIndex);
        }
        return RolledTraits;
    }

    // Tuto metodu zavolá tlačítko SPIN
    public void StartSpin()
    {
        // Pokud už hráč jednou rolloval, dál ho to nepustí
        if (hasSpun) return;

        hasSpun = true;
        if (spinButton != null) spinButton.interactable = false; // Natrvalo vypne tlačítko Spin

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        // 1. Předem vylosujeme 3 finální unikátní traity
        RollTraits(3);
        ActiveTraits = new List<Trait>(RolledTraits);

    List<Trait> allValues = new List<Trait>(allTraits.Values);
        float elapsed = 0f;
        float waitTime = 0.04f;

   
        while (elapsed < spinDuration)
        {
            for (int i = 0; i < slotTexts.Length; i++)
            {
                Trait randomVisualTrait = allValues[Random.Range(0, allValues.Count)];
                slotTexts[i].text = randomVisualTrait.Name;
            }

            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
            waitTime += 0.015f;
        }

        // 3. Zastavení na finálních 3 vylosovaných traitech
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (i < RolledTraits.Count)
            {
                slotTexts[i].text = RolledTraits[i].Name;
            }
        }

     
    }

    public void GoToGame()
    {

        if (!hasSpun) {
            return; 
        
        } 
        SceneManager.LoadScene(2);
    }
}