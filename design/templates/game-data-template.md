# Game Data Template

## dataset: items
```json
[
  {
    "id": "wooden_pickaxe",
    "type": "tool",
    "name": "Wooden Pickaxe",
    "stack_max": 1,
    "durability": 59,
    "groups": ["tool", "pickaxe", "wood"]
  },
  {
    "id": "plank",
    "type": "material",
    "name": "Wood Planks",
    "stack_max": 64,
    "groups": ["material", "wood", "flammable"]
  },
  {
    "id": "stick",
    "type": "material",
    "name": "Stick",
    "stack_max": 64,
    "groups": ["material", "wood", "flammable"]
  },
  {
    "id": "wheat",
    "type": "material",
    "name": "Wheat",
    "stack_max": 64,
    "groups": ["material", "grain", "plant"]
  },
  {
    "id": "iron_ore",
    "type": "material",
    "name": "Iron Ore",
    "stack_max": 64,
    "groups": ["material", "ore"]
  },
  {
    "id": "iron_ingot",
    "type": "material",
    "name": "Iron Ingot",
    "stack_max": 64,
    "groups": ["material", "metal", "iron"]
  },
  {
    "id": "coal",
    "type": "resource",
    "name": "Coal",
    "stack_max": 64,
    "groups": ["resource", "fuel"]
  },
  {
    "id": "bucket",
    "type": "tool",
    "name": "Bucket",
    "stack_max": 16,
    "groups": ["tool", "container"]
  },
  {
    "id": "milk_bucket",
    "type": "food",
    "name": "Milk Bucket",
    "stack_max": 1,
    "groups": ["food", "container", "bucket_filled"]
  },
  {
    "id": "bread",
    "type": "food",
    "name": "Bread",
    "stack_max": 64,
    "hunger_restore": 5,
    "groups": ["food", "edible"]
  },
  {
    "id": "cake",
    "type": "food",
    "name": "Cake",
    "stack_max": 1,
    "hunger_restore": 14,
    "groups": ["food", "edible"]
  }
]
```

## dataset: recipes
```json
[
  {
    "id": "recipe_wooden_pickaxe",
    "name": "Wooden Pickaxe",
    "method": "NORMAL",
    "craft_time": 0.0,
    "shaped": true,
    "width": 3,
    "height": 3,
    "station": "crafting_table",
    "results": [
      { "item_id": "wooden_pickaxe", "amount": 1 }
    ],
    "ingredients": [
      { "group": "group:wood", "amount": 3 },
      { "item_id": "stick", "amount": 2 }
    ],
    "replacements": []
  },
  {
    "id": "recipe_bread",
    "name": "Bread",
    "method": "NORMAL",
    "craft_time": 0.0,
    "shaped": false,
    "results": [
      { "item_id": "bread", "amount": 1 }
    ],
    "ingredients": [
      { "group": "group:grain", "amount": 3 }
    ],
    "replacements": []
  },
  {
    "id": "recipe_cake",
    "name": "Cake",
    "method": "NORMAL",
    "craft_time": 0.0,
    "shaped": true,
    "width": 3,
    "height": 3,
    "station": "crafting_table",
    "results": [
      { "item_id": "cake", "amount": 1 }
    ],
    "ingredients": [
      { "item_id": "milk_bucket", "amount": 3 },
      { "item_id": "wheat", "amount": 3 }
    ],
    "replacements": [
      { "from": "milk_bucket", "to": "bucket" }
    ]
  },
  {
    "id": "recipe_iron_ingot",
    "name": "Iron Ingot",
    "method": "COOKING",
    "craft_time": 10.0,
    "results": [
      { "item_id": "iron_ingot", "amount": 1 }
    ],
    "ingredients": [
      { "item_id": "iron_ore", "amount": 1 }
    ],
    "replacements": []
  },
  {
    "id": "recipe_coal_fuel",
    "name": "Coal Fuel",
    "method": "FUEL",
    "craft_time": 80.0,
    "results": [],
    "ingredients": [
      { "item_id": "coal", "amount": 1 }
    ],
    "replacements": []
  }
]
```

## dataset: monsters
```json
[
  { "id": "zombie", "tier": 1, "health": 20, "attack": 3, "speed": 0.23, "drops": ["rotten_flesh"] },
  { "id": "skeleton", "tier": 1, "health": 20, "attack": 4, "speed": 0.25, "drops": ["bone", "arrow"] },
  { "id": "creeper", "tier": 1, "health": 20, "attack": 12, "speed": 0.25, "drops": ["gunpowder"] }
]
```

## dataset: npcs
```json
[
  { "id": "villager_farmer", "role": "farmer", "shop_tier": 1, "dialogue_pool": ["hello_farmer_01", "trade_offer_01"] },
  { "id": "villager_blacksmith", "role": "blacksmith", "shop_tier": 2, "dialogue_pool": ["hello_smith_01", "trade_offer_05"] }
]
```

## dataset: character_stats
```json
{
  "base": {
    "max_health": 20,
    "max_hunger": 20,
    "move_speed": 0.1,
    "attack_power": 1
  },
  "growth_per_level": {
    "max_health": 1,
    "attack_power": 0.3
  }
}
```

