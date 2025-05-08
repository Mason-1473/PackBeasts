using UnityEngine;
using UnityEngine.UI;

public class RoleUI : MonoBehaviour
{
    public Image roleIcon;
    public Text roleNameText;
    public Button dayAbilityButton;
    public Button duskAbilityButton1;
    public Button duskAbilityButton2;
    public Button nightAbilityButton1;
    public Button nightAbilityButton2;
    private RoleAsset role;
    private RoleManager roleManager;

    void Awake()
    {
         roleManager = FindObjectOfType<RoleManager>();
    if (dayAbilityButton != null)
        dayAbilityButton.onClick.AddListener(() => roleManager.UseAbility(role.dayAbility));
    if (duskAbilityButton1 != null)
        duskAbilityButton1.onClick.AddListener(() => roleManager.UseAbility(role.duskAbility1));
    if (duskAbilityButton2 != null)
        duskAbilityButton2.onClick.AddListener(() => roleManager.UseAbility(role.duskAbility2));
    if (nightAbilityButton1 != null)
        nightAbilityButton1.onClick.AddListener(() => roleManager.UseAbility(role.nightAbility1));
    if (nightAbilityButton2 != null)
        nightAbilityButton2.onClick.AddListener(() => roleManager.UseAbility(role.nightAbility2));
    }

    void Update()
    {
         // Enable/disable buttons based on current phase
    if (dayAbilityButton != null)
        dayAbilityButton.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Day && role.dayAbility != AbilityType.None);
    if (duskAbilityButton1 != null)
        duskAbilityButton1.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Dusk && role.duskAbility1 != AbilityType.None);
    if (duskAbilityButton2 != null)
        duskAbilityButton2.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Dusk && role.duskAbility2 != AbilityType.None);
    if (nightAbilityButton1 != null)
        nightAbilityButton1.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Night && role.nightAbility1 != AbilityType.None);
     if (nightAbilityButton2 != null)
        nightAbilityButton2.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Night && role.nightAbility2 != AbilityType.None);
    }

    public void SetRole(RoleAsset roleAsset)
    {
        role = roleAsset;
        if (roleIcon != null && role.icon != null)
            roleIcon.sprite = role.icon;
        if (roleNameText != null)
            roleNameText.text = role.roleName;
    }
}