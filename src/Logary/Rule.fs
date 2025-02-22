namespace Logary

open System
open System.Text.RegularExpressions

open Logary
open Logary.Internals

/// This is the accept filter that is before the message is passed to the logger
type MessageFilter = Message -> bool

/// A rule specifies what messages a target should accept.
[<CustomEquality; CustomComparison>]
type Rule =
  { /// This is the regular expression that the 'path' must match to be loggable
    hiera         : Regex
    /// This is the name of the target that this rule applies to.
    target        : PointName
    /// This is the level at which the target will accept log lines. It's inclusive, so
    /// anything below won't be accepted.
    level         : LogLevel
    /// This is the accept filter that is before the message is passed to the logger
    /// instance.
    messageFilter : MessageFilter }

  override x.GetHashCode () =
    hash (x.hiera.ToString(), x.target, x.level)

  override x.Equals other =
    match other with
    | null -> false
    | :? Rule as o -> (x :> IEquatable<Rule>).Equals(o)
    | _ -> false

  interface System.IEquatable<Rule> with
    member x.Equals r =
      r.hiera.ToString() = x.hiera.ToString()
      && r.target = x.target
      && r.level = x.level

  interface System.IComparable with
    member x.CompareTo yobj =
      match yobj with
      | :? Rule as y ->
        compare (x.hiera.ToString()) (y.hiera.ToString())
        |> thenCompare x.target y.target
        |> thenCompare x.level y.level
      | _ -> invalidArg "yobj" "cannot compare values of different types"

  override x.ToString() =
    sprintf "Rule(hiera=%O, target=%O, level=%O)"
      x.hiera x.target x.level

/// Module for dealing with rules. Rules take care of filtering too verbose
/// log lines and measures before they are sent to the targets.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Rule =

  // filters:
  /// A filter that accepts any input given
  let allowFilter _ = true

  /// Find all rules matching the name, from the list of rules passed.
  let matching (name : PointName) (rules : Rule list) =
    rules |> List.filter (fun r -> r.hiera.IsMatch (PointName.format name))

  /////////////////////
  // Creating rules: //
  /////////////////////

  let private allHiera = Regex(".*", RegexOptions.Compiled)

  /// An empty rule; don't forget to give it a proper name or the configuration
  /// won't work, e.g. using the `createForTarget` method.
  let empty =
    { hiera         = allHiera
      target        = PointName.empty
      messageFilter = fun _ -> true
      level         = Verbose }

  let setHiera (regex : Regex) (r : Rule) =
    { r with hiera = regex }

  let setHieraS (regex : string) (r : Rule) =
    { r with hiera = Regex(regex) }

  let setTarget (target : PointName) (r : Rule) =
    { r with target = target }

  let setMessageFilter (mf : _ -> _) (r : Rule) =
    { r with messageFilter = mf }

  let setLevel (l : LogLevel) (r : Rule) =
    { r with level = l }

  /// Create a rule that accepts any input for a specified target (that's the
  /// name param).
  let createForTarget (name : PointName) =
    { empty with target = name }

  /// Create a new rule with the given hiera, target, accept function and min level
  /// acceptable.
  let create hiera target messageFilter level =
    { hiera         = hiera
      target        = target
      messageFilter = messageFilter
      level         = level }