using System.Collections.Generic;
using UnityEngine;

public static class CreatureDuelDebugUnitRegistry
{
    private sealed class Entry
    {
        public Object Source;
        public readonly List<Unit> Units = new();
    }

    private static readonly List<Entry> Entries = new();

    public static void Register(Object source, List<Unit> units)
    {
        if (source == null)
            return;

        Entry entry = FindEntry(source);
        if (entry == null)
        {
            entry = new Entry { Source = source };
            Entries.Add(entry);
        }

        entry.Units.Clear();
        if (units == null)
            return;

        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (unit == null)
                continue;

            if (ContainsUnit(entry.Units, unit))
                continue;

            entry.Units.Add(unit);
        }
    }

    public static void Unregister(Object source)
    {
        if (source == null)
            return;

        for (int i = Entries.Count - 1; i >= 0; i--)
        {
            Entry entry = Entries[i];
            if (entry == null || ReferenceEquals(entry.Source, source))
                Entries.RemoveAt(i);
        }
    }

    public static IReadOnlyList<Unit> GetUnitsFor(Unit owner)
    {
        if (owner == null)
            return null;

        for (int i = 0; i < Entries.Count; i++)
        {
            Entry entry = Entries[i];
            if (entry == null || entry.Source == null)
                continue;

            if (ContainsUnit(entry.Units, owner))
                return entry.Units;
        }

        return null;
    }

    private static Entry FindEntry(Object source)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            Entry entry = Entries[i];
            if (entry != null && ReferenceEquals(entry.Source, source))
                return entry;
        }

        return null;
    }

    private static bool ContainsUnit(List<Unit> units, Unit candidate)
    {
        if (units == null || candidate == null)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            if (ReferenceEquals(units[i], candidate))
                return true;
        }

        return false;
    }
}
