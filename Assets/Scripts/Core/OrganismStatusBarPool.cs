using System.Collections.Generic;
using UnityEngine;

public class OrganismStatusBarPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private OrganismStatusBar prefab;
    [SerializeField] private Transform poolParent;
    [SerializeField] private int initialSize = 32;
    [SerializeField] private bool expandPool = true;

    private readonly Stack<OrganismStatusBar> available = new();
    private readonly HashSet<OrganismStatusBar> allBars = new();

    private void Awake()
    {
        if (poolParent == null)
            poolParent = transform;

        Prewarm();
    }

    private void Prewarm()
    {
        for (int i = 0; i < initialSize; i++)
            CreateBar();
    }

    private OrganismStatusBar CreateBar()
    {
        OrganismStatusBar bar = Instantiate(prefab, poolParent);

        bar.gameObject.SetActive(false);

        available.Push(bar);
        allBars.Add(bar);

        return bar;
    }

    public OrganismStatusBar Get()
    {
        if (available.Count == 0)
        {
            if (!expandPool)
                return null;

            CreateBar();
        }

        OrganismStatusBar bar = available.Pop();

        bar.gameObject.SetActive(true);

        return bar;
    }

    public void Release(OrganismStatusBar bar)
    {
        if (bar == null)
            return;

        if (!allBars.Contains(bar))
            return;

        bar.Unbind();

        bar.gameObject.SetActive(false);

        bar.transform.SetParent(poolParent, false);

        available.Push(bar);
    }

    public void ReleaseAll()
    {
        foreach (OrganismStatusBar bar in allBars)
        {
            if (bar == null)
                continue;

            bar.Unbind();
            bar.gameObject.SetActive(false);
            bar.transform.SetParent(poolParent, false);
        }

        available.Clear();

        foreach (OrganismStatusBar bar in allBars)
        {
            if (bar != null)
                available.Push(bar);
        }
    }
}