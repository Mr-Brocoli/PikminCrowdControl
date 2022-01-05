# PikminCrowdControl
Pikmin 1 Crowd Control for Pikmin USA v 1.0.1

All effects designed to work with Pikmin 1 Multiplayer: https://allreds.itch.io/pikmin-multiplayer

Effect Descriptions:

(Blue,Red,Yellow) = BRY

BRY Pikmin Color: A bidding war per Pikmin that lets you decide each of their colours. Notice that the colour change does not effect particles or textures revolving around the Pikmin.

(Flower,Bud,Leaf) = FBL

FBL Field Pikmin: Makes every Pikmin on the field, not those in the onion or buried, become FBL accordingly.

FBL speeds are originally (270,240,220) in game units.

Fast Pikmin: Makes Pikmin become +100 in game units faster. This decision was made as doubling speeds causes just a bit of glitching, where as +100 is more reasonable.

Slow Pikmin: Makes Pikmin become 50% of their speed.

BRY strengths = (10,15,10). Strength is a Pikmin's damage per hit on any item or enemy.

Strong Pikmin: BRY strengths now equal (15,20,15)

Weak Pikmin: BRY strengths now equal (5,7.5,5)

GAMEFLOW + 0x2f8 contains a 'dayTimeInt' to represent each chunk of time on the day timer. -0x7-0x6 is the unused night time mode. 0x7 - 0x13 is day time. The day ends once the dayTimeInt hits 0x13.

Forward Time: Adds +1 to the dayTimeInt, forwarding the day by 1/12. Notice that different levels in the game have different amounts of time so this is fractional of that day time.

Rewind Time: Subs -1 to the dayTimeInt, giving the streamer an extra 1/12 of day. Rewind Time will be delayed if the dayTimeInt is set to 0.

Disable Whistle: Renders Olimar's whistle invisible and its ability to call Pikmin.

Unused Pluckaphone feature: https://www.youtube.com/watch?v=fxBiTw5EURM

Grant Pluckaphone: Toggle on the unused pluckaphone feature

Revoke Pluckaphone: Toggle off the unused pluckaphone feature

Heal Olimar: Sets Olimar's health to 100, which happens to be his max health.

One-Hit KO: Set's Olimar's health to 1, literally anything will kill him.

Disable Hud: The Hud will temporarily become completely disabled, good luck knowing what you have in your squad.

Hyper Olimar: Doubles Olimar's movement speed to 320.

Lethargic Olimar: Half's Olimar's movement speed to 80.

Send Olimar to Spawn: Teleports Olimar back to the starting co ordinates of the map, which can be some serious trolling on behalf of the donator.

Thanos Snap: Kills half of the Pikmin on field indiscriminantly, bringing balance to the universe.

Invincible Pikmin: Makes Pikmin invincible to everything except drowning.

Invisibile Pikmin: Makes Pikmin invisible to everything except drowning.

"Wiimote Controls": Triples Olimar's whistle range and whistle speed kind of like how the wii mote works in the wii version. Just a silly idea I had but I like it. 
