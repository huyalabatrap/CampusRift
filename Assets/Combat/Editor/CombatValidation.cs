using System.Collections.Generic;
using System.IO;
using CampusRift.Combat;
using UnityEditor;
using UnityEngine;

// Pure-logic checks for the V2 damage pipeline (P01-T01, P01-T02). Edit or Play Mode.
public static class CombatValidation
{
    [System.Serializable] sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
    static Report report;
    static void Check(bool ok, string label) { (ok ? report.passed : report.failed).Add(label); }

    sealed class Dummy : IDamageable
    {
        public Element Element { get; set; }
        public bool IsDead => false;
        public Transform Anchor => null;
        public bool ApplyDamage(DamageInfo info) => true;
    }

    [MenuItem("Campus Rift/V2/Validate Element Chart")]
    public static string ElementChartAndCalculator()
    {
        report = new Report();
        var all = (Element[])System.Enum.GetValues(typeof(Element));
        var five = new[] { Element.Kim, Element.Moc, Element.Tho, Element.Thuy, Element.Hoa };
        int cells = 0;
        foreach (var a in all)
        foreach (var t in all)
        {
            cells++;
            float expected = 1f;
            if (a != Element.None && t != Element.None)
            {
                if (a == Element.Loi && t == Element.Am) expected = 1.5f;
                else if (a == Element.Kim && t == Element.Am) expected = 0.8f;
                else
                {
                    int ia = System.Array.IndexOf(five, a), it = System.Array.IndexOf(five, t);
                    if (ia >= 0 && it >= 0)
                    {
                        if ((ia + 1) % 5 == it) expected = 1.5f;       // Kim▶Mộc▶Thổ▶Thủy▶Hỏa▶Kim
                        else if ((it + 1) % 5 == ia) expected = 0.75f;
                    }
                }
            }
            float actual = ElementChart.Multiplier(a, t);
            if (!Mathf.Approximately(actual, expected)) Check(false, "Multiplier " + a + " vs " + t + " = " + actual + ", expected " + expected);
        }
        Check(cells == 81, "Element chart covers all 81 attack/target pairs");
        if (report.failed.Count == 0) Check(true, "All 81 multipliers match the plan (§7.2)");

        var cycle = new[] { Element.Moc, Element.Hoa, Element.Tho, Element.Kim, Element.Thuy };
        bool generation = true;
        for (int i = 0; i < 5; i++) generation &= ElementChart.Generates(cycle[i], cycle[(i + 1) % 5]) && !ElementChart.Generates(cycle[(i + 1) % 5], cycle[i]);
        Check(generation, "Five tương sinh pairs Mộc→Hỏa→Thổ→Kim→Thủy→Mộc, one direction only");
        Check(!ElementChart.Generates(Element.Loi, Element.Moc) && !ElementChart.Generates(Element.None, Element.None), "Lôi/Âm/None take no part in tương sinh");

        var rng = new System.Random(7);
        var tho = new Dummy { Element = Element.Tho };
        var kim = new Dummy { Element = Element.Kim };
        var am = new Dummy { Element = Element.Am };
        Check(Mathf.Approximately(DamageCalculator.Compute(20, 1f, Element.Moc, tho, 0, 1.5f, rng).amount, 30), "Basic Mộc attack on Thổ: 20 × 100% × 1.5 = 30");
        Check(Mathf.Approximately(DamageCalculator.Compute(20, 1f, Element.Moc, kim, 0, 1.5f, rng).amount, 15), "Mộc on Kim is resisted: 20 × 0.75 = 15");
        Check(Mathf.Approximately(DamageCalculator.Compute(34, 4.5f, Element.Hoa, kim, 0, 1.5f, rng).amount, 229.5f), "Phật Nộ Hỏa Liên at Trúc Cơ 1 on Thiết Giáp: 34 × 450% × 1.5 = 229.5 (§16)");
        var crit = DamageCalculator.Compute(100, 2f, Element.Loi, am, 1f, 1.5f, rng);
        Check(crit.critical && Mathf.Approximately(crit.amount, 450), "Guaranteed critical Lôi on Âm: 100 × 200% × 1.5 × 1.5 = 450");
        Check(!DamageCalculator.Compute(100, 1f, Element.None, null, 0f, 2f, rng).critical, "Zero crit chance never crits");
        Check(Mathf.Approximately(DamageCalculator.AfterDefense(100, 0.2f), 80) && Mathf.Approximately(DamageCalculator.AfterDefense(100, 2f), 20),
            "Defense removes its fraction and is capped at 80%");
        var env = DamageInfo.Create(10, Element.Hoa, DamageSource.Environment, Vector3.zero, Vector3.down);
        var melee = DamageInfo.Create(10, Element.None, DamageSource.Melee, Vector3.zero, Vector3.down);
        Check(env.ignoreInvulnerability && !melee.ignoreInvulnerability, "Environment damage ignores hit invulnerability; melee does not");

        Directory.CreateDirectory("Artifacts/Combat");
        File.WriteAllText("Artifacts/Combat/ElementChart.json", JsonUtility.ToJson(report, true));
        string result = "ELEMENT QA " + (report.failed.Count == 0 ? "PASS" : "FAIL") + ": " + report.passed.Count + " passed / " + report.failed.Count + " failed " + string.Join("; ", report.failed);
        Debug.Log(result);
        return result;
    }
}
