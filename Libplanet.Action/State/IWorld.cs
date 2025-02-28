using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;

namespace Libplanet.Action.State
{
    /// <summary>
    /// An interface to manipulate an world state with
    /// maintaining <see cref="Delta"/>.
    /// <para>It is like a map which is virtually initialized such
    /// that every possible <see cref="Address"/> exists and
    /// is mapped to <see langword="null"/>.  That means that:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>it does not have length,</description>
    /// </item>
    /// <item>
    /// <description>its index getter never throws
    /// <see cref="KeyNotFoundException"/>,
    /// but returns <see langword="null"/> instead, and</description>
    /// </item>
    /// <item>
    /// <description>filling an <see cref="Address"/> with
    /// <see langword="null"/> account cannot be distinguished from
    /// the <see cref="Address"/> having never been set to
    /// any account.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// This interface is immutable.  <see cref="SetAccount"/>
    /// method does not manipulate the instance, but returns a new
    /// <see cref="IWorld"/> instance with updated states.
    /// </remarks>
    public interface IWorld : IWorldState
    {
        /// <summary>
        /// The <see cref="IWorld"/> representing the delta part of
        /// this <see cref="IWorld"/>.
        /// </summary>
        [Pure]
        IWorldDelta Delta { get; }

        /// <summary>
        /// A set of <see cref="Address"/> and <see cref="Currency"/> pairs where
        /// each pair has its asoociated <see cref="FungibleAssetValue"/> changed
        /// since the previous <see cref="Block"/>'s output states.
        /// </summary>
        [Pure]
        IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets { get; }

        /// <summary>
        /// Gets the <see cref="IAccount"/> of the given <paramref name="address"/>.
        /// </summary>
        /// <param name="address">The <see cref="Address"/> referring
        /// the world to get its state.</param>
        /// <returns>The <see cref="IAccount"/> of the given <paramref name="address"/>.
        /// If it has never been set to any state it returns <see langword="null"/>
        /// instead.</returns>
        [Pure]
        IAccount GetAccount(Address address);

        /// <summary>
        /// Creates a new instance of <see cref="IWorld"/> with given <paramref name="address"/>
        /// set to given <paramref name="account"/>.
        /// </summary>
        /// <param name="address">The <see cref="Address"/> for which to set
        /// given <see cref="account"/> to.</param>
        /// <param name="account">The new <see cref="IAccount"/> to set to
        /// given <paramref name="address"/>.</param>
        /// <returns>A new <see cref="IWorld"/> instance where the account state of given
        /// <paramref name="address"/> is set to given <paramref name="account"/>.</returns>
        /// <remarks>
        /// This method method does not manipulate the instance, but returns
        /// a new <see cref="IWorld"/> instance with an updated world state instead.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="Legacy"/> is <see langword="true"/> and
        /// <paramref name="address"/> is not <see cref="ReservedAddresses.LegacyAccount"/>.
        /// </exception>
        [Pure]
        IWorld SetAccount(Address address, IAccount account);

        /// <summary>
        /// Mints the fungible asset <paramref name="value"/> (i.e., in-game monetary),
        /// and give it to the <paramref name="recipient"/>.
        /// </summary>
        /// <param name="context">The <see cref="IActionContext"/> of the <see cref="IAction"/>
        /// executing this method.</param>
        /// <param name="recipient">The address who receives the minted asset.</param>
        /// <param name="value">The asset value to mint.</param>
        /// <returns>A new <see cref="IWorld"/> instance that the given <paramref
        /// name="value"/> is added to <paramref name="recipient"/>'s balance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/>
        /// is less than or equal to 0.</exception>
        /// <exception cref="CurrencyPermissionException">Thrown when a transaction signer
        /// (or a miner in case of block actions) is not a member of the <see
        /// cref="FungibleAssetValue.Currency"/>'s <see cref="Currency.Minters"/>.</exception>
        /// <exception cref="SupplyOverflowException">Thrown when the sum of the
        /// <paramref name="value"/> to be minted and the current total supply amount of the
        /// <see cref="FungibleAssetValue.Currency"/> exceeds the
        /// <see cref="Currency.MaximumSupply"/>.</exception>
        [Pure]
        IWorld MintAsset(IActionContext context, Address recipient, FungibleAssetValue value);

        /// <summary>
        /// Burns the fungible asset <paramref name="value"/> (i.e., in-game monetary) from
        /// <paramref name="owner"/>'s balance.
        /// </summary>
        /// <param name="context">The <see cref="IActionContext"/> of the <see cref="IAction"/>
        /// executing this method.</param>
        /// <param name="owner">The address who owns the fungible asset to burn.</param>
        /// <param name="value">The fungible asset <paramref name="value"/> to burn.</param>
        /// <returns>A new <see cref="IWorld"/> instance that the given <paramref
        /// name="value"/> is subtracted from <paramref name="owner"/>'s balance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/>
        /// is less than or equal to zero.</exception>
        /// <exception cref="CurrencyPermissionException">Thrown when a transaction signer
        /// (or a miner in case of block actions) is not a member of the <see
        /// cref="FungibleAssetValue.Currency"/>'s <see cref="Currency.Minters"/>.</exception>
        /// <exception cref="InsufficientBalanceException">Thrown when the <paramref name="owner"/>
        /// has insufficient balance than <paramref name="value"/> to burn.</exception>
        [Pure]
        IWorld BurnAsset(IActionContext context, Address owner, FungibleAssetValue value);

        /// <summary>
        /// Transfers the fungible asset <paramref name="value"/> (i.e., in-game monetary)
        /// from the <paramref name="sender"/> to the <paramref name="recipient"/>.
        /// </summary>
        /// <param name="context">The <see cref="IActionContext"/> of the <see cref="IAction"/>
        /// executing this method.</param>
        /// <param name="sender">The address who sends the fungible asset to
        /// the <paramref name="recipient"/>.</param>
        /// <param name="recipient">The address who receives the fungible asset from
        /// the <paramref name="sender"/>.</param>
        /// <param name="value">The asset value to transfer.</param>
        /// <param name="allowNegativeBalance">Turn on to allow <paramref name="sender"/>'s balance
        /// less than zero.  Turned off by default.</param>
        /// <returns>A new <see cref="IWorld"/> instance that the given <paramref
        /// name="value"/>  is subtracted from <paramref name="sender"/>'s balance and added to
        /// <paramref name="recipient"/>'s balance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/>
        /// is less than or equal to zero.</exception>
        /// <exception cref="InsufficientBalanceException">Thrown when the <paramref name="sender"/>
        /// has insufficient balance than <paramref name="value"/> to transfer and
        /// the <paramref name="allowNegativeBalance"/> option is turned off.</exception>
        /// <remarks>
        /// The behavior is different depending on <paramref name="context"/>'s
        /// <see cref="IActionContext.BlockProtocolVersion"/>.  There is a bug for version 0
        /// where this may not act as intended.  Such behavior is left intact for backward
        /// compatibility.
        /// </remarks>
        [Pure]
        IWorld TransferAsset(
            IActionContext context,
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false);

        /// <summary>
        /// Sets <paramref name="validator"/> to the stored <see cref="ValidatorSet"/>.
        /// If 0 is given as its power, removes the validator from the <see cref="ValidatorSet"/>.
        /// </summary>
        /// <param name="validator">The <see cref="Validator"/> instance to write.</param>
        /// <returns>A new <see cref="IWorld"/> instance with
        /// <paramref name="validator"/> set.</returns>
        [Pure]
        IWorld SetValidator(Validator validator);
    }
}
