# 20220228

* (hopefully) fix converting pack CONs to RBVR with ForgeTool (@InvoxiPlayGames)

# 20220225

Rock Band VR support by InvoxiPlayGames.

* Adds "con2rbvr" verb to ForgeTool, which converts a CON custom into an RBVR custom.
* Changes DtxCS to use my fork which adds DTB serialization.
* Writes `{split_ark}` DTA command to arkorders obtained using the "arkorder" verb.
* Adds file flags to the output arkorders and adjusts ARK building procedure to take flags into account.
* Allows specifying xor value for HDR encryption during ARK building.
* RBMid "PART_KEYS" parsing is not included in debug builds as they still cause crashes.
* Adds information on "con2rbvr", "arkorder" and "arkbuild" verbs in ForgeTool's usage section.

# 20220115

Minor bug fixes by Onyxite.

* Fix cases where an unintended kick lane would be created on drums
* Fix cases where the fake and crowd channel indices aren't assigned correctly
* Don't require a numerically sorted list of channel indices in the input
