using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using System;

namespace ImmersiveCrafting
{
  public class BlockBehaviorUseToolThenRemoveBlock : BlockBehavior
  {
    string actionlangcode;
    int toolDurabilityCost;
    JsonItemStack outputStack;
    EnumTool[] toolTypes;
    string[] toolTypesStrTmp;
    WorldInteraction[] interactions;

    public BlockBehaviorUseToolThenRemoveBlock(Block block) : base(block)
    {
    }

    public override void OnLoaded(ICoreAPI api)
    {
      base.OnLoaded(api);

      toolTypes = new EnumTool[toolTypesStrTmp.Length];
      for (int i = 0; i < toolTypesStrTmp.Length; i++)
      {
        if (toolTypesStrTmp[i] == null) continue;
        try
        {
          toolTypes[i] = (EnumTool)Enum.Parse(typeof(EnumTool), toolTypesStrTmp[i]);
        }
        catch (Exception)
        {
          api.Logger.Warning("UseToolThenRemoveBlock behavior for block {0}, tool type {1} is not a valid tool type, will default to knife", block.Code, toolTypesStrTmp[i]);
          toolTypes[i] = EnumTool.Knife;
        }
      }
      toolTypesStrTmp = null;

      interactions = ObjectCacheUtil.GetOrCreate(api, "useToolThenRemoveBlockInteractions-", () =>
      {
        // List<ItemStack> toolStacks = new List<ItemStack>();

        // foreach (CollectibleObject obj in api.World.Items)
        // {
        //   if (obj.Tool == EnumTool.Knife || obj.Tool == EnumTool.Sword)
        //   {
        //     toolStacks.Add(new ItemStack(obj));
        //   }
        // }

        return new WorldInteraction[]
        {
          new WorldInteraction()
          {
            ActionLangCode = actionlangcode,
            MouseButton = EnumMouseButton.Right,
            // Itemstacks = toolStacks.ToArray()
          }
        };
      });
    }

    public override void Initialize(JsonObject properties)
    {
      base.Initialize(properties);

      actionlangcode = properties["actionLangCode"].AsString();
      toolDurabilityCost = properties["toolDurabilityCost"].AsInt();
      outputStack = properties["outputStack"].AsObject<JsonItemStack>();
      toolTypesStrTmp = properties["toolTypes"].AsArray<string>(new string[0]);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
      ItemStack outputstack = null;
      if (outputStack.Resolve(world, "output stacks"))
        outputstack = outputStack.ResolvedItemstack;

      ItemStack itemslot = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

      if (CanUseHeldTool(toolTypes, byPlayer, itemslot))
      {
        itemslot.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, toolDurabilityCost);

        if (!byPlayer.InventoryManager.TryGiveItemstack(outputstack))
        {
          world.SpawnItemEntity(outputstack, byPlayer.Entity.Pos.XYZ);
        }

        world.BlockAccessor.SetBlock(0, blockSel.Position);

        handling = EnumHandling.PreventDefault;
      }
      return true;
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
    {
      handling = EnumHandling.PassThrough;
      return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling));
    }

    private bool CanUseHeldTool(EnumTool[] toolTypes, IPlayer byPlayer, ItemStack itemslot)
    {
      if (itemslot?.Collectible.GetDurability(itemslot) >= toolDurabilityCost)
      {
        EnumTool? targetTool = itemslot?.Collectible.Tool;
        for (int i = 0; i < toolTypes.Length; i++) if (targetTool == toolTypes[i]) return true;
      }
      return false;
    }
  }
}
