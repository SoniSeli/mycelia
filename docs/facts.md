# Fact cards

One fact per level, shown in full on the result panel after a win (design §3.6, ADR-0012 Refinements). Facts and photos were chosen by Soni Seli: the theme is what mycelium can do, as food, material and fashion, and for the climate.

*Status 2026-10-24: in the level assets as **placeholder text** until the Milestone 5 content pass. The photos are **local only** (`Assets/ThirdParty/FactPhotos/`, git-ignored): their licences are unknown, so they are never committed and must be licensed or replaced before any public build, store listing or README media.*

| Level | Fact | Photo source |
|---|---|---|
| level-01 | Mycelium is the hidden rootlike network of fungi, a web of tiny threads (hyphae) spreading through soil. It grows into dead wood and leaves, breaking them down. This decomposition releases nutrients back into the earth, feeding new plant and mushroom growth. | miro.medium.com |
| level-02 | In forests, trees actually connect through mycelium. This underground "wood wide web" lets trees swap nutrients and even signal each other to dangers. For example, birch and fir trees exchange carbon and water via these fungal links, showing plants cooperate underground for forest health. | plantgrowsave.org |
| level-03 | Mycelium makes delicious, sustainable food. By fermenting fungi on simple plant waste, companies create protein-rich mycoprotein ingredients (think the brand Quorn). This fungal protein has all essential amino acids, plus fiber and vitamins, but no saturated fat or cholesterol. It's a meat-free superfood! | blogs.imperial.ac.uk |
| level-04 | Mycelium can grow into bricks and panels. By mixing fungal spores with agricultural waste (like straw or sawdust) in molds, you get solid building blocks. Once dried, these mycelium bricks are surprisingly strong and fireproof, and they biodegrade safely after use. | parametric-architecture.com |
| level-05 | Mycelium can replace plastics in packaging. For example, Ecovative's Mushroom® Packaging grows fungus on waste fibers to form custom box shapes. The result is a foam-like material that cushions products yet fully composts back to earth, unlike Styrofoam. | cdn.shopify.com (a shop) |
| level-06 | These mycelium products have bonus superpowers: they naturally resist water and fire due to their biology. For instance, mycelium insulation is both hydrophobic and flame-resistant. This means safer packaging and building materials without needing synthetic chemicals. | buildinggreen.com |
| level-07 | Mycelium can be turned into fashion like vegan "leather." Companies grow fungi on trays of sawdust to make sheets of leather-like mycelium. After drying and processing, these sheets are dyed and cut into shoes, bags or jackets - a flexible, vegan alternative to animal leather. | indiebio.co (MycoWorks) |
| level-08 | The fashion industry is already experimenting with mycelium. Brands have made shoe and handbag prototypes, and designers weave fungus fibers into new fabrics. The material feels soft and velvety, almost like suede, yet is completely plant-based and biodegradable. | idtxs3.imgix.net |
| level-09 | Mycelium foods don't need farmland or long seasons. One startup's mycelium grows in a tank in just 8–10 days (versus months for soy or years for beef), using minimal land, energy and water. The fungus naturally tastes a bit meaty, making plant-based foods more savory and appealing. | oakandspore.co.nz |
| level-10 | Because mycelium is grown (not drilled or refined), its carbon footprint is tiny. One study found that using mycelium insulation instead of concrete could turn buildings into carbon sinks, storing CO2 in the wood-based material. In short: the walls breathe in carbon instead of releasing it. | BBC (ychef.files.bbci.co.uk) |
| level-11 | Mycelium is a circular-economy superstar. Growing products on waste materials (like hemp stalks or corn hulls) and later composting them means nothing goes to landfill. This loop cuts trash and returns nutrients to nature, helping fight pollution and climate change. | Springer Nature (journal figure) |
| level-12 | Mycelium products blend industry and nature, ticking many green checkboxes. They cut waste and carbon (helping SDGs 12 and 13) and spark new innovation (SDG 9). In plain terms, using mushroom materials is like nature's high-tech hack: fun, practical, and better for the planet. | Springer Nature (journal figure) |

The full photo URLs are in Soni's notes next to the photos (`Assets/ThirdParty/FactPhotos/`). The card's credit line shows the source site, and only when the photo is present.

**Import settings (decided 2026-10-24, Milestone 3 slice 8):** on Android each photo is capped at 1024 px and compressed as ASTC 6 × 6. With the automatic setting, seven photos whose sides aren't multiples of 4 shipped uncompressed, which put the release APK at 43.0 MB, over its 40 MB budget; now it is 38.7 MB ([performance budget](performance-budget.md)). The card shows a photo 380 px tall. The settings live in the photos' `.meta` files, which are git-ignored with them, so a new copy of the photos needs them set again. Soni's reason for doing it before Milestone 5: it's a cheap fix.

## For the Milestone 5 content pass

Decided 2026-10-24 to keep the text as written for playtesting (*why:* it is placeholder text until the content pass). Points raised in review, to check then:

- **level-02:** trees "signalling each other to dangers" through mycelium is disputed; a 2023 review (Karst et al., *Nature Ecology & Evolution*) found the evidence for such networks overstated.
- **level-03:** Quorn's mycoprotein is grown in fermenters on glucose, not plant waste, and is low in saturated fat rather than free of it.
- **level-04:** mycelium bricks are light and non-load-bearing; "fire-resistant" fits better than "strong and fireproof".
- **level-08:** fungi are not plants; "plant-based" contradicts the game's own biology ("fungus-based").
- **level-09, level-10:** need a source; level-10 calls mycelium "wood-based".
- **Sources:** each fact needs a source, as the photos need a licence.
