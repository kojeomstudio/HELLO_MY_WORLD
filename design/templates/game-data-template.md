# Game Data Template

## dataset: items
```json
[
  { "id": "wooden_pickaxe", "type": "tool", "durability": 59, "stackable": false },
  { "id": "bread", "type": "food", "hunger_restore": 5, "stackable": true, "max_stack": 64 },
  { "id": "iron_ingot", "type": "material", "stackable": true, "max_stack": 64 }
]
```

## dataset: recipes
```json
[
  {
    "id": "recipe_wooden_pickaxe",
    "result": { "item_id": "wooden_pickaxe", "count": 1 },
    "ingredients": [
      { "item_id": "plank", "count": 3 },
      { "item_id": "stick", "count": 2 }
    ]
  },
  {
    "id": "recipe_bread",
    "result": { "item_id": "bread", "count": 1 },
    "ingredients": [
      { "item_id": "wheat", "count": 3 }
    ]
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

