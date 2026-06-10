# Creature Variant Tool Guide

The **Creature Variant Tool** is a comprehensive, editor-friendly window in the Unity Editor designed to streamline the cataloging, searching, generation, and validation of creature variant assets generated from the V2 Skill Composition system.

---

## 1. What is the Tool?
* **Name**: Creature Variant Tool
* **Menu Path**: `Tools/Creatures/Creature Variant Tool`
* **Purpose**: Allows designers and developers to view, search, filter, generate/regenerate, and validate all playable and enemy creature variants from a single workspace.

---

## 2. Core Concepts
A creature variant is a combination of a base template (defining stats and visuals) and a formal skill composition:
* **Faction**: `Human` or `Orc`
* **Role**: `DPS`, `Tank`, or `Support`
* **Skill Composition**: A combination of Delivery (Impact Pattern), Effect, and Modifiers.

### Variant Naming Formula
$$\text{Faction} + \text{Role} + \text{SkillNameWithoutRolePrefix} \rightarrow \text{VariantName}$$

* **Example**: `Human` + `DPS` + `DirectDamage` $\rightarrow$ `Human_DPS_DirectDamage`

---

## 3. Catalog Structure
The variants are organized under `Assets/Core/Data/Scriptable Objects/Creatures/Generated/`:
* `Generated/Human/`: Active Human variants grouped by role.
* `Generated/Orc/`: Active Orc variants grouped by role.
* `Generated/_Excluded/`: Inactive/Experimental variants that do not meet production requirements.

### Catalog Stats
* **Active Variants**: **54** (27 Human, 27 Orc)
* **Excluded Variants**: **38** (19 Human, 19 Orc)
* **Status**: **0 Invalid Active Variants**

> [!IMPORTANT]
> Assets placed in the `_Excluded/` directory are not indexed by recruitment pools or standard encounters unless explicitly configured for experimental testing.

---

## 4. Filters Available
The tool provides filters to quickly browse variants:
* **Faction**: `All`, `Human`, or `Orc`
* **Role**: `All`, `DPS`, `Tank`, or `Support`
* **Delivery**: `All`, `Direct` (Single Target), `Area` (Splash/Radius), or `Line`
* **Effect**: `All`, `Damage`, `Heal`, `Shield`, `Status`, `Summon`, or `Knockback`
* **Status Subtype**: `All`, `Stun`, `Slow`, `PoisonBurn`, `Haste`, `StrengthBuff`, or `HealOverTime` (ticks)
* **Modifier**: `All`, `None`, `Splash`, `Penetrating`, `Bounce`, `Explosive`, `Periodic`, or `Persistent`
* **Search Text**: Filters results by variant or skill name (case-insensitive).
* **Show Excluded Toggle**: If checked, reveals the 38 experimental variants in `_Excluded/`.

---

## 5. Result Columns
The variants table lists:
1. **Variant name**: The name of the variant asset.
2. **Faction**: The unit's faction.
3. **Role**: The combat role (DPS/Tank/Support).
4. **Skill**: The official skill name.
5. **Delivery**: The impact pattern.
6. **Effects**: List of effects with values/duration.
7. **Modifiers**: List of modifiers applied.
8. **State**: `Active` (green) or `Excluded` (grey).

---

## 6. Action Buttons
* **Top Header Buttons**:
  * **Generate / Regenerate Variants**: Re-runs the generator script to create new variants or update existing ones based on templates.
  * **Validate Variants**: Runs the validation suite over all active variants, verifying prefabs and skill-role matches.
  * **Refresh Browser**: Re-scans disk assets to update the table contents.
* **Row Actions**:
  * **Select**: Selects the variant asset in the Unity Inspector.
  * **Ping U**: Highlights the `UnitData` asset in the Project window.
  * **Ping S**: Highlights the `SkillData` asset in the Project window.
  * **Copy**: Copies the database path of the variant asset to the clipboard.

---

## 7. Recommended Search Workflows
Use the dropdown filters to find specific variants:
* **To find `Human_DPS_DirectDamage`**:
  Select Faction `Human`, Role `DPS`, Delivery `Direct`, Effect `Damage`, Modifier `None`.
* **To find `Orc_Tank_AreaSlow`**:
  Select Faction `Orc`, Role `Tank`, Delivery `Area`, Effect `Status`, Status Subtype `Slow`.
* **To find `Human_Support_AreaBuffSpeed`**:
  Select Faction `Human`, Role `Support`, Delivery `Area`, Effect `Status`, Status Subtype `Haste`.

---

## 8. Encounter Design Guidelines
When designing encounters:
1. Use the **Creature Variant Tool** to browse available active combat behaviors.
2. Use the **Copy** action to get the exact path of the `UnitData` asset to insert into your level/encounter config.
3. Keep initial pools small to balance gameplay before scaling.
4. **Do not use `_Excluded` variants** in main level encounters.

### Recommended Early Enemy Pool
* `Orc_DPS_DirectDamage` (Standard melee damage dealer)
* `Orc_DPS_DirectKnockback` (Melee with knockback control)
* `Orc_Tank_AreaShield` (Applies shield to nearby allies)
* `Orc_Tank_AreaSlow` (Slowing presence)
* `Orc_Support_AllyHeal` (Heals wounded enemies)
* `Orc_Support_AreaShield` (Buffer backline)

---

## 9. Recruitment Flow Guidelines
When integrating variants into the player's recruitment mechanics:
* Implement recruitment based on the **exact variant name** defeated in combat.
* Maintain a pool of **permitted active variants** for character selection.
* **Avoid permitting `_Excluded` variants** as they might cause runtime issues or visual glitches.

---

## 10. When to Use "Generate / Regenerate"
Only run the generator in these situations:
* After adding a new `Official` skill to a role folder.
* After updating the properties of an existing base UnitData template.
* After changing variant naming conventions or folder hierarchies.
* *Do not run it if you only want to search or read paths.*

---

## 11. When to Use "Validate Variants"
Run the validator in these situations:
* Immediately after generating/regenerating variants.
* Before integrating variants into encounters or recruitment pools.
* Before pushing/committing changes to git.

---

## 12. Best Practices & Restrictions (What NOT to Do)
> [!CAUTION]
> Avoid these common pitfalls to maintain catalog sanity.

* **No Manual Overwrites**: Do not modify generated variant assets directly in the Inspector. If stats or skills need changing, change the base UnitData template or the underlying SkillData asset, then run **Generate / Regenerate**.
* **No Manual Filesystem Movement**: Do not drag generated variant assets to other folders via the filesystem. Use the generator tools to categorize and move obsolete ones to `_Excluded/` automatically.
* **No Experimental Integration**: Do not use `_Excluded/` variants in production campaigns.
* **No Duplication**: Do not duplicate variants manually to create new ones; always define a new `SkillData` asset first.
* **No Legacy Backend Reintroduction**: Do not attempt to assign old `SkillEffect` or `SkillModifier` components. All configurations must be done via the V2 Skill Composition system.

---

## 13. Healthy Catalog Baseline
A healthy project state must meet the following criteria:
* **Active Variants**: exactly 54.
* **Invalid Active Variants**: 0.
* **Validate Composition Metadata**: 0 critical violations (Rules 1-6, 9-10).
* **Validate Composition Runtime Readiness**: 0 active gaps.
* `_Excluded` variants are hidden by default in the tool browser.
