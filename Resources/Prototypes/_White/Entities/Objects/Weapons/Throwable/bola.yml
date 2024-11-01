- type: entity
  parent: BaseBola
  id: Bola
  name: bola
  description: Linked together with some spare cuffs and metal.
  components:
  - type: Construction
    graph: Bola
    node: bola
  - type: Damageable
    damageContainer: Inorganic
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 75
      behaviors:
        - !type:DoActsBehavior
          acts: [ "Destruction" ]
    - trigger:
        !type:DamageTrigger
        damage: 15
      behaviors:
      - !type:PlaySoundBehavior
        sound:
          collection: MetalBreak
      - !type:DoActsBehavior
        acts: [ "Destruction" ]
  - type: DamageOnLand
    damage:
      types:
        Blunt: 5

- type: entity
  parent: BaseBola
  id: EnergyBola
  name: energy bola
  description: A perfect fusion of technology and justice to catch criminals.
  components:
  - type: Item
    size: Small
  - type: Sprite
    sprite: _White/Objects/Weapons/Throwable/energybola.rsi
  - type: EmitSoundOnLand
    sound:
      collection: sparks
  - type: KnockdownOnCollide
    knockdownTime: 2
    jitterTime: 7
    stutterTime: 7
  - type: Tag
    tags:
      - SecBeltEquip
