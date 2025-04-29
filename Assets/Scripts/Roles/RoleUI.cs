using UnityEngine;
using UnityEngine.UI;

public class RoleUI : MonoBehaviour
{
    public Image roleIcon;
    public Text roleNameText;
    public Button dayAbilityButton;
    public Button duskAbilityButton;
    public Button nightAbilityButton;
    private RoleAsset role;
    private RoleManager roleManager;

    void Awake()
    {
        roleManager = FindObjectOfType<RoleManager>();
        if (dayAbilityButton != null)
            dayAbilityButton.onClick.AddListener(() => roleManager.UseAbility(role.dayAbility));
        if (duskAbilityButton != null)
            duskAbilityButton.onClick.AddListener(() => roleManager.UseAbility(role.duskAbility));
        if (nightAbilityButton != null)
            nightAbilityButton.onClick.AddListener(() => roleManager.UseAbility(role.nightAbility));
    }

    void Update()
    {
        // Enable/disable buttons based on current phase
        if (dayAbilityButton != null)
            dayAbilityButton.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Day && role.dayAbility != AbilityType.None);
        if (duskAbilityButton != null)
            duskAbilityButton.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Dusk && role.duskAbility != AbilityType.None);
        if (nightAbilityButton != null)
            nightAbilityButton.gameObject.SetActive(GameManager.Instance.currentPhase == GamePhase.Night && role.nightAbility != AbilityType.None);
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