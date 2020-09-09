using Unity.MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    [SerializeField] private float updateSpeedSeconds = 0.5f;

    private void Awake()
    {
        GetComponentInParent<GameCharacter>().OnHealthChanged += HandleHealthChanged;
    }

    private void HandleHealthChanged(float percentage)
    {
        StartCoroutine(ChangeToPercentage(percentage));
    }

    /// <summary>
    /// Given a specific percentage, change image current fillAmount to match it
    /// </summary>
    /// <param name="percentage"> New percentage of the health left </param>
    private IEnumerator ChangeToPercentage(float percentage)
    {
        float preChangePercentage = foregroundImage.fillAmount;
        float elapsed = 0f;

        while (elapsed < updateSpeedSeconds)
        {
            elapsed += Time.deltaTime;
            foregroundImage.fillAmount = Mathf.Lerp(preChangePercentage, percentage, elapsed / updateSpeedSeconds);
            yield return null;
        }

        foregroundImage.fillAmount = percentage;
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
