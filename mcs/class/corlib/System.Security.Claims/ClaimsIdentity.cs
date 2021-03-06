//
// ClaimIdentity.cs
//
// Authors:
//  Miguel de Icaza (miguel@xamarin.com)
//
// Copyright 2014 Xamarin Inc
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#if NET_4_5
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Runtime.Serialization;
namespace System.Security.Claims {

	[Serializable]
	public class ClaimsIdentity : IIdentity {
		[NonSerializedAttribute]
		public const string DefaultNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
		[NonSerializedAttribute]
		public const string DefaultRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
		[NonSerializedAttribute]
		public const string DefaultIssuer = "LOCAL AUTHORITY";
		
		List<Claim> claims;
		ClaimsIdentity actor;
		string auth_type;

		public ClaimsIdentity ()
			: this (claims: null, authenticationType: null, nameType: null, roleType: null)
		{ }
		
		public ClaimsIdentity (string authenticationType)
			: this (claims: null, authenticationType: authenticationType, nameType: null, roleType: null)
		{ }

		public ClaimsIdentity (IEnumerable<Claim> claims, string authenticationType) 
			: this (claims, authenticationType, null, null)
		{}
		
		public ClaimsIdentity (string authenticationType, string nameType, string roleType)
			: this (claims: null, authenticationType: authenticationType, nameType: nameType, roleType: roleType)
		{ }
		
		public ClaimsIdentity (IIdentity identity) : this (identity: identity, claims: null)
		{
		}
		
		public ClaimsIdentity(IEnumerable<Claim> claims, string authenticationType, string nameType, string roleType)
			: this (identity: null, claims: claims, authenticationType: authenticationType, nameType: nameType, roleType: roleType)
		{
			claims = claims == null ? new List<Claim> (): new List<Claim> (claims);
			
			// Special case: if empty, set to null.
			if (authenticationType == "")
				auth_type = null;
			else
				auth_type = authenticationType;

			NameClaimType = nameType == null ? DefaultNameClaimType : nameType;
			RoleClaimType = roleType == null ? DefaultRoleClaimType : roleType;
		}

		public ClaimsIdentity (IIdentity identity, IEnumerable<Claim> claims)
			: this (identity, claims, authenticationType: null, nameType: null, roleType: null)
		{ }
		
		public ClaimsIdentity (IIdentity identity, IEnumerable<Claim> claims, string authenticationType, string nameType, string roleType)
		{
			var ci = identity as ClaimsIdentity;
			NameClaimType = nameType == null ? DefaultNameClaimType : nameType;
			RoleClaimType = roleType == null ? DefaultRoleClaimType : roleType;
			
			this.claims = new List<Claim> ();
			if (ci != null){
				actor = ci.Actor;
				BootstrapContext = ci.BootstrapContext;
				foreach (var c in ci.Claims)
					this.claims.Add (c);
				
				Label = ci.Label;
				NameClaimType = ci.NameClaimType;
				RoleClaimType = ci.RoleClaimType;
				auth_type = ci.AuthenticationType;
			}

			if (claims != null) {
				foreach (var c in claims)
					this.claims.Add (c);
			}
		}

		[MonoTODO]
		protected ClaimsIdentity (SerializationInfo info)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		protected ClaimsIdentity (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException ("info");
			throw new NotImplementedException ();
		}
		
		public ClaimsIdentity Actor {
			get {
				return actor;
			}
			set {
				if (actor == this)
					throw new InvalidOperationException ("can not set the Actor property to this instance");
				actor = value;
			}
		}

		public virtual string AuthenticationType {
			get {
				return auth_type;
			}
		}
		public object BootstrapContext { get; set; }
		public string Label { get; set; }
		public virtual string Name {
			get {
				var target = NameClaimType;
				foreach (var c in claims){
					if (c.Type == target)
						return c.Value;
				}
				return null;
			}
		}
		public string NameClaimType { get; private set; }
		public string RoleClaimType { get; private set; }

		public virtual IEnumerable<Claim> Claims {
			get {
				return claims;
			}
		}

		public virtual bool IsAuthenticated {
			get {
				return AuthenticationType != null && AuthenticationType != "";
			}
		}

		public virtual void AddClaim (Claim claim)
		{
			if (claim == null)
				throw new ArgumentNullException ("claim");
			claims.Add (claim);
		}

		public virtual void AddClaims (IEnumerable<Claim> claims)
		{
			if (claims == null)
				throw new ArgumentNullException ("claims");
			foreach (var c in claims)
				this.claims.Add (c);
		}

		public virtual ClaimsIdentity Clone ()
		{
			return new ClaimsIdentity (null, claims, AuthenticationType, NameClaimType, RoleClaimType){
				BootstrapContext = this.BootstrapContext,
				Actor = this.Actor,
				Label = this.Label
			};
		}

		public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
		{
			if (match == null)
				throw new ArgumentNullException ("match");
			foreach (var c in claims)
				if (match (c))
					yield return c;
		}

		public virtual IEnumerable<Claim> FindAll(string type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			foreach (var c in claims)
				if (c.Type == type)
					yield return c;
		}

		public virtual Claim FindFirst (Predicate<Claim> match)
		{
			if (match == null)
				throw new ArgumentNullException ("match");
			foreach (var c in claims)
				if (match (c))
					return c;
			return null;
		}

		public virtual Claim FindFirst (string type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			foreach (var c in claims)
				if (c.Type == type)
					return c;
			return null;
		}

		public virtual bool HasClaim (Predicate<Claim> match)
		{
			if (match == null)
				throw new ArgumentNullException ("match");
			foreach (var c in claims)
				if (match (c))
					return true;
			return false;
		}

		public virtual bool HasClaim (string type, string value)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (value == null)
				throw new ArgumentNullException ("value");
			foreach (var c in claims){
				if (c.Type == type && c.Value == value)
					return true;
			}
			return false;
		}

		public virtual void RemoveClaim (Claim claim)
		{
			if (!TryRemoveClaim (claim))
				throw new InvalidOperationException ();
		}

		[MonoTODO ("This one should return false if the claim is owned by someone else, this does not exist yet")]
		public virtual bool TryRemoveClaim (Claim claim)
		{
			if (claim == null)
				return true;
			claims.Remove (claim);
			return true;
		}
	}
}
#endif
