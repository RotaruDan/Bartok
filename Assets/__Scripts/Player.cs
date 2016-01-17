using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

// The player can either be human or an ai
public enum PlayerType
{
	human,
	ai
}

// The individual player of the game
// Note: Player does NOT extend MonoBehaviour (or any other class)

[System.Serializable] // Make the Player class visible in the Inspector pane
public class Player
{

	public PlayerType type = PlayerType.ai;
	public int playerNum;

	public List<CardBartok> hand; // The cards in this player's hand

	public SlotDef handSlotDef;

	// Add a card to the hand
	public CardBartok AddCard(CardBartok eCB)
	{
		if (hand == null) hand = new List<CardBartok>();

		// Add the card to the hand
		hand.Add(eCB);
		// Sort the cards by rank using LINQ if this is a human
		if (type == PlayerType.human)
		{
			CardBartok[] cards = hand.ToArray(); // Copy hand to a new array

			// Below is the LINQ call that works on the array of CardBartoks.
			//  It is similar to doing a foreach(CardBartok cd in cards)
			//  and sorting them by rank. It then returns a sorted array
			cards = cards.OrderBy(cd => cd.rank).ToArray();

			// Convert the array CardBartok[] back to a List<CardBartok>
			hand = new List<CardBartok>(cards);
			// Note: LINQ operations can be a bit slow (like it could take a
			//  couple of milliseconds), but since we're only doing it once
			//  every turn, it isn't a problem.
		}

		eCB.SetSortingLayerName("10"); // This sorts the moving card to the top 
		eCB.eventualSortLayer = handSlotDef.layerName;

		FanHand();

		return (eCB);
	}

	// Remove a card from the hand
	public CardBartok RemoveCard(CardBartok cb)
	{
		hand.Remove(cb);
		FanHand();

		return (cb);
	}

	public void FanHand()
	{
		// startRot is the rotation about Z of the first card
		float startRot = 0;
		startRot = handSlotDef.rot;
		if (hand.Count > 1)
		{
			startRot += Bartok.S.handFanDegrees * (hand.Count - 1) / 2;
		}
		// Then each card is rotated handFanDegrees from that to fan the cards

		// Move all the cards to their new positions
		Vector3 pos;
		float rot;
		Quaternion rotQ;
		for (int i = 0; i < hand.Count; i++)
		{
			rot = startRot - Bartok.S.handFanDegrees * i; // Rot about the z axis
														  // ^ Also adds the rotations of the different players' hands
			rotQ = Quaternion.Euler(0, 0, rot);
			// ^ Quaternion representing the same rotation as rot

			// pos is a V3 half a card height above [0,0,0] (i.e., [0,1.75,0])
			pos = Vector3.up * CardBartok.CARD_HEIGHT / 2f;

			// Multiplying a Quaternion by a Vector3 rotates that Vector3 by
			//  the rotation stored in the Quaternion. The result gives us a
			//  vector above [0,0,0] that has been rotated by rot degrees
			pos = rotQ * pos;

			// Add the base position of the player's hand (which will be at the
			//  bottom-center of the fan of the cards)
			pos += handSlotDef.pos;
			// This staggers the cards in the z direction, which isn't visible
			//  but which does keep their colliders from overlapping
			pos.z = -0.5f * i;

			// The line below makes sure that the card starts moving immediately
			//  if it's not the initial deal at the beginning of the game.
			if (Bartok.S.phase != TurnPhase.idle)
			{
				hand[i].timeStart = 0;
			}

			// Set the localPosition and rotation of the ith card in the hand
			hand[i].MoveTo(pos, rotQ); // Tell CardBartok to interpolate
			hand[i].state = CBState.toHand;
			// ^ After the move, CardBartok will set the state to CBState.hand
			/*
			hand[i].transform.localPosition = pos;
			hand[i].transform.rotation = rotQ;
			hand[i].state = CBState.hand;
			*/

			// This uses a comparison operator to return a true or false bool
			// So, if (type == PlayerType.human), hand[i].faceUp is set to true
			hand[i].faceUp = (type == PlayerType.human);

			// Set the SortOrder of the cards so that they overlap properly
			hand[i].eventualSortOrder = i * 4;
			//hand[i].SetSortOrder(i*4);
		}

	}

	// The TakeTurn() function enables the AI of the computer Players
	public void TakeTurn()
	{
		Utils.tr(Utils.RoundToPlaces(Time.time), "Player.TakeTurn");

		// Don't need to do anything if this is the human player.
		if (type == PlayerType.human) return;

		Bartok.S.phase = TurnPhase.waiting;

		CardBartok cb;

		// If this is an AI player, need to make a choice about what to play
		// Find valid plays
		List<CardBartok> validCards = new List<CardBartok>();
		foreach (CardBartok tCB in hand)
		{
			if (Bartok.S.ValidPlay(tCB))
			{
				validCards.Add(tCB);
			}
		}
		// If there are no valid cards
		if (validCards.Count == 0)
		{
			// ...then draw a card
			cb = AddCard(Bartok.S.Draw());
			cb.callbackPlayer = this;
			return;
		}

		// Otherwise, if there is a card or more to play, pick one
		cb = validCards[Random.Range(0, validCards.Count)];
		RemoveCard(cb);
		Bartok.S.MoveToTarget(cb);
		cb.callbackPlayer = this;

	}

	public void CBCallback(CardBartok tCB)
	{
		Utils.tr(Utils.RoundToPlaces(Time.time), "Player.CBCallback()", tCB.name, "Player " + playerNum);
		// The card is done moving, so pass the turn
		Bartok.S.PassTurn();
	}
}