#!/bin/bash
# MIRROR NOTE 2026-08-26: the personal-name banks LIVE in TerraFirma/XML/Naming.xml
# now (the migration landed them in the schema this script was always mimicking);
# byname banks + grants live in Builder_StonecarverEpithet.cs. This roller keeps its
# copies as the design-side demo tool - port changes rather than trusting these.
# draft banks chopped from mysteriousetruscans.com/names.html
# structure mimics Naming.xml: prefix x1, infix x0-2, postfix x1, TwoNameChance
# scrubbed banks: junction architecture (prefix C-final, infix VCV, postfix C-initial)
# crimes removed in draft: ar+thur=Arthur, vin+ce=Vince, se/her (Secure/Here/Hera)
prefixes=(ar vel lar than ram sem cul tles pev ras mun spur)
infixes=(utha athi eni ena ari ithi aru eli anthi elthu arna uni avi)
postfixes=(tha thia na nia ce za til sa cna)
# two-axis banks (13x13 draft 2026-08-15, pending user pass): material grants perk, craft grants kit
fam_prefixes=(stone flint scree cairn grit chalk spall marl cobble salt karst quartz lode)
fam_postfixes=(smith augur wright knapper reeve ward delver graver scribe reader carter setter slinger)

perk_of() {
  case "$(tr '[:upper:]' '[:lower:]' <<< "$1")" in
    stone*) echo "+1 Str, +1 Tou";; flint*) echo "+2 Agi";; scree*) echo "+1-2 AV";;
    cairn*) echo "+2 Ego";; grit*) echo "+2 Will";; chalk*) echo "+2 Int";;
    spall*) echo "+2 DV";; marl*) echo "+HP";; cobble*) echo "+MoveSpeed";;
    salt*) echo "heat 50";; karst*) echo "cold 50";; quartz*) echo "elec 50";;
    lode*) echo "wealth";;
  esac
}
kit_of() {
  case "$1" in
    *smith|*knapper|*delver) echo "Fissure";;
    *wright|*setter|*graver) echo "Stoneshaper";;
    *ward|*carter|*slinger) echo "Earthen Barrage";;
    *augur|*reader) echo "Geomancy";;
    *reeve) echo "office: +Ego/+Will";;
    *scribe) echo "office: +Int/+Will";;
  esac
}

cap() { echo "$(tr '[:lower:]' '[:upper:]' <<< ${1:0:1})${1:1}"; }

roll_personal() {
  # no guard needed anymore — banks are collision-proof by construction
  local n="${prefixes[$((RANDOM % ${#prefixes[@]}))]}"
  (( RANDOM % 2 )) && n+="${infixes[$((RANDOM % ${#infixes[@]}))]}"   # 0-1 infixes
  n+="${postfixes[$((RANDOM % ${#postfixes[@]}))]}"
  cap "$n"
}

roll_family() {
  local n="${fam_prefixes[$((RANDOM % ${#fam_prefixes[@]}))]}"
  n+="${fam_postfixes[$((RANDOM % ${#fam_postfixes[@]}))]}"
  cap "$n"
}

for i in $(seq 1 12); do
  p=$(roll_personal)
  f=$(roll_family)
  # byname is hero-epithet-only in game (ruled 2026-08-15); demo shows the full sheet
  printf "%-26s -> %s | %s\n" "$p $f" "$(kit_of "$f")" "$(perk_of "$f")"
done
