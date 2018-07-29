﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MHArmory.Core.DataStructures
{
    public interface IHasAbilities
    {
        IAbility[] Abilities { get; }
    }

    public interface IEquipment : IHasAbilities
    {
        int Id { get; }
        EquipmentType Type { get; }
        string Name { get; }
        int Rarity { get; }
        int[] Slots { get; }
    }
}
