# LiveSplit.EldenRaceCounter

LiveSplit plugin to automatically count points of an [EldenRace](https://arshesl.notion.site/6d3f8eae9d994bce813ef57753ac5630?v=33f75a56fc924cf6b136d0948e773baa).

It is based on the [Livesplit.Counter](https://github.com/LiveSplit/LiveSplit.Counter) plugin and depends on [SoulMemory](https://github.com/FrankvdStam/SoulSplitter/tree/main/src/SoulMemory) to interact with the game instance.

> Note: Currently only bosses can be used.

## Options

- `Configuration`: `csv` file with to columns separated by `,`

    - The first column is the name of the event from SoulMemory enums ([Boss](https://github.com/FrankvdStam/SoulSplitter/blob/main/src/SoulMemory/EldenRing/Boss.cs), [ItemPickup](https://github.com/FrankvdStam/SoulSplitter/blob/main/src/SoulMemory/EldenRing/ItemPickup.cs) - Only Bosses used for now)
    - The second is the amount of points to add when the event occurs

    Example:

    ```csv
    GodrickTheGraftedStormveilCastle,10
    MargitTheFellOmenStormveilCastle,10
    GraftedScionChapelOfAnticipation,2
    ```
- `Write default configuration`: output the configuration `csv` with the correct format with 2 categories (default major bosses: 10pts and all others 2pts).
- `Randomizer mapping`: use the `spoiler_logs/.*.txt` file from the [Enemy randomizer mode](https://www.nexusmods.com/eldenring/mods/428). If the default configuration is not used, it will correctly map the boss points with the randomizer.

