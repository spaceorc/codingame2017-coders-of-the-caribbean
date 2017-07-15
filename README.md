# codingame2017-coders-of-the-caribbean
Solution for https://www.codingame.com/contests/coders-of-the-caribbean

## Game
It's a solution itself

- *Entities* - all supported entities
- *Geometry* - implementation of hexagonal coords (see http://www.redblobgames.com/grids/hexagons/), positions of ships on the map and it's transformations. *FastCoord* and *FastShipPosition* are bitmasks that use precalculated tables of distances and transformations
- *Navigation* - finding best paths to required targets
- *Prediction* - builds forecasts for next turns - damage maps, ship positions, enemies that can fire or drop mines etc. Used in many other solution parts, such as *Navigation*, *FireTeam*, etc
- *FireTeam* - cannoneers and miners
- *State* - turn state and game state
- *Strategy* - most important part of solution - strategies for playing 1 vs 1, 2 vs 2 and 3 vs 3

## Implemented strategies

- *Strategy1vs1*. When there are some barrels, it predicts the enemy path over them and tries to collect barrels before the enemy collects'em, and shots barrels if it can not collect'em before the enemy does. When there are no more barrels, it runs away (yes, independently of the current level of rum - such feature as chasing the enemy was not implemented :( )
- *Strategy2vs2* is pretty the same as *Strategy1vs1*, but with some improvement: if skippers see that they are loosing the game, one of them makes harakiri, giving the other some rum.
- *Strategy3vs3* - each skipper collects barrels with a greedy algorithm. When there is no more barrels - strategy is the same as *Strategy2vs2* - to make harakiri if needed

## Experiments
Project for experiments. Entry point for replaying game turns from dumps
