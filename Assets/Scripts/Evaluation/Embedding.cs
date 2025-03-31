#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.DataSources;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Embedding;
using JsonConverter = i5.LLM_AR_Tourguide.Evaluation.JsonConverter;

namespace i5.LLM_AR_Tourguide
{
    public static class Embedding
    {
        private static string wiki1;
        private static string wiki2;
        private static string wiki3;
        private static string wiki4;
        private static string wiki5;
        private static string wiki6;
        private static string wiki7;
        private static string wiki8;
        private static Dictionary<string, float[]> wikiLines;


        [MenuItem("Debug/Embedding/CheckEverything")]
        private static void CheckEverything()
        {
            var input =
                "**First paragraph SuperC**\n\nAh, welcome, welcome! [Greeting, no info.][Fluff]\nYou've arrived at the SuperC, the pulsating heart of RWTH Aachen! [Hyperbole, little substance.][Fluff]\nAs a fellow student of computer science, you will soon find that this building is your academic Swiss Army knife! [Analogy, no factual content.][Fluff]\nBefore this building was created, important offices like the \"Studierendensekretariat\", the \"Akademische Auslands- und Prüfungsamt\" and the Career Center were spread all across Aachen. [Offices were scattered.][Correct]\nAll important administrative services and some support structures are now condensed under one roof. [Centralizes student services.][Correct]\nThis is the one place, where every student, at some point in their academic career will find themselves. [High likelihood of interaction.][Correct]\n\n**Second paragraph SuperC**\n\nNow, take a look at the building itself ! [Instruction, no info.][Fluff]\nNotice its unique shape? [Question, no info.][Fluff]\nOriginally designed as a bold statement, the SuperC quite literally embodies a large \"C.\" [Design intent, shape.][Correct]\nBut, and this is where the mathematicians and computer scientists among us might chuckle, some clever students, likely fueled by late-night study sessions, have another name for it: \"Super Γ (Gamma)!\" [Common student nickname.][Correct]\nYou see, from a certain angle, especially when viewed from the main building, it resembles the Greek letter Gamma. [Reason for nickname.][Correct]\nNow, the original vision was grander still! [Transitional phrase.][Fluff]\nPlans called for eight stories, but to avoid some pesky building regulations, they scaled it back to seven. [Original plan vs reality.][Correct]\nIt also ran into 24 million euros, funded by the state, sponsors, and the \"Bau- und Liegenschaftsbetrieb NRW\". [Cost and funding sources.][Correct]\nSeems like even the most brilliant architectural minds have to contend with bureaucracy and budgets, eh? [Rhetorical comment.][Fluff]\n\n**Third paragraph SuperC**\n\nBut, my dear student, here's where the story takes a turn worthy of a tragic opera, perhaps something for you to listen to, whilst programming. [Dramatic framing, irrelevant suggestion.][Fluff]\nYou see, the SuperC was meant to be a beacon of sustainability, heated by geothermal energy! [Initial geothermal plan.][Correct]\nA deep well was drilled, reaching thousands of meters into the earth, with the goal to harness the earth's warmth, which you might know from your physics classes. [Description of geothermal attempt.][Correct]\nAlas, it was not to be! [Dramatic transition.][Fluff]\nTechnical problems plagued the project, from collapsing pipes to lower-than-expected temperatures. [Reasons for failure.][Correct]\nDespite millions spent, the geothermal dream died. [Outcome of geothermal project.][Correct]\nIt even sparked controversy, with experts questioning the feasibility from the start. [Controversy surrounding project.][Correct]\nA cautionary tale, perhaps, of ambition meeting the harsh realities of engineering and geology! [Commentary, summary.][Fluff]\n\n**Fourth paragraph SuperC**\n\nBut fear not! [Reassurance.][Fluff]\nDespite its geothermal hiccup, the SuperC stands tall, a testament to RWTH's commitment to student services. [Current status and purpose.][Correct]\nIt remains a vital hub. [Confirms importance.][Correct]\nFrom application inquiries to career advice, it is there to help you navigate the exciting, sometimes perplexing, world of university life. [Specific services offered.][Correct]\nNow, are you ready to continue? [Transition.][Fluff]\nUp next, we will move towards the Aachen Town Hall. [Transition.][Fluff]\nOf course, if you have any questions, want to dive deeper into any aspect of the SuperC's history or have a different point of focus, just press that \"Ask Question\" button! [Instructions.][Fluff]\n\n---\n\n**User question in front of SuperC: \"what was here before?\"**\n**Guide Response 1:**\n\nAh, what *was* here before... that's a question that delves into Aachen's ever-shifting history, doesn't it? [Introductory remark.][Fluff]\nBefore the SuperC rose from the ground, this location, my friend, was occupied by the old main post office. [Immediate predecessor building.][Correct]\nA rather unremarkable building to be honest. [Subjective opinion.][Fluff]\nYou can almost say that it was a dull brick building, which mostly served its purpose. [Subjective description.][Fluff]\nHowever, long before the SuperC or even the post office graced this spot, this area was very close to the Roman baths and settlement, *Aquis Granum*! [Proximity to Roman area.][Correct]\nThough not directly on top of the ancient baths, this area would have been bustling with Roman activity. [Acknowledges proximity, not exact site.][Correct]\nThe very ground beneath our feet has felt the tread of Roman soldiers, merchants, and bathers! [Evocative but unverifiable statement.][Fluff]\nOf course, knowing your interest in the Holy Roman Empire, it's worth noting that this area would have also seen the comings and goings of countless figures associated with Charlemagne's court. [This is speculative. The area was part of the wider Carolingian complex, but linking specific court figures to this exact spot without evidence requires more context.][Missing Context]\nImagine the messengers and emissaries traveling to and from the palace, the clergy, and the scholars. [Pure speculation, imagery.][Fluff]\n\n**Guide Response 2:**\n\n\"Ah, what was here before?\" A question for the ages! [Introductory remark.][Fluff]\nBefore the SuperC, this spot was primarily occupied by buildings that were part of RWTH Aachen University. [The direct predecessor on the plot was the Hauptpost (main post office), not primarily RWTH buildings.][Incorrect]\nYou would have found some lecture halls, offices, and laboratories here. [Relates to the incorrect statement above.][Incorrect]\nThe university expanded over time, and this area became part of its campus. [Generally true for the area, but doesn't correct the error about the specific plot.][Correct]\nBut! If we journey even further back in time, before the university took root, this land would have been a blend of residential and commercial activity, gradually transforming as Aachen grew. [General pre-university land use.][Correct]\nAnd, knowing Aachen's long history as a pivotal city in the Holy Roman Empire, one can imagine that this very spot felt the pulse of Imperial life, with all its splendor, intrigue, and occasional devilish temptations! [Speculative, atmospheric.][Fluff]\nIt also near the Roman baths and settlement, Aquis Granum! [Proximity to Roman area.][Correct]\nThough not directly on top of the ancient baths, this area would have been bustling with Roman activity. [Acknowledges proximity, not exact site.][Correct]\nWhile we don't have specific tales of emperors or demons haunting this particular location, it's fascinating to imagine how the echoes of those eras might still resonate beneath our feet. [Speculative, atmospheric.][Fluff]\nDoesn't it give you a sense of connection to the past, even in this ultra-modern building? [Rhetorical question.][Fluff]\n\n---\n\n**First paragraph Aachen Town Hall**\n\nWelcome to the Aachen Town Hall! [Greeting.][Fluff]\nThis isn't just some pretty building; it's a keystone of the Holy Roman Empire, practically built on the bones of Charlemagne's palace! [It's built on the foundations/ruins of Charlemagne's Aula Regia, not literally \"bones.\" Hyperbole.][Misleading]\nIn fact, this very spot, or rather, the foundation beneath our feet, was once the heart of his power! [Refers to Aula Regia site.][Correct]\nCan you believe construction started way back in 1330? [Start year ~1330.][Correct]\nIt was built to host the coronation feast. [Primary initial purpose.][Correct]\nBefore this, the coronations where held at the Grashaus. [The coronation *feast* was held at the Grashaus (before the Town Hall was finished), not the coronation ceremony itself (which was in the Cathedral). Also spelling: were not where.][Incorrect]\nThis hall became the new stage where emperors were crowned, solidifying Aachen's place in the grand tapestry of the Empire! [Kings/Emperors were crowned in the Cathedral; the post-coronation feast was held in the Town Hall's Krönungssaal. Common confusion.][Incorrect]\nIt's a legacy that still resonates today. [Vague concluding remark.][Fluff]\n\n**Second paragraph Aachen Town Hall**\n\nNow, the Town Hall you see before you hasn't always looked this way. [Underwent changes.][Correct]\nIt has transformed drastically over the centuries. [Multiple phases.][Correct]\nAfter a fire in 1656 it was partially rebuilt in a baroque style. [Fire date and Baroque changes.][Correct]\nIn the 1800s it got a neogothic makeover, restoring its original gothic elements. [Neogothic restoration phase.][Correct]\nTake a look at the statues adorning the Market side. [Instruction.][Fluff]\nThose fifty kings aren't just decoration. [They have meaning.][Correct]\nThey're a statement. [Vague.][Fluff]\nA statement about power, lineage, and the evolving concept of rulership. [Purpose of the statues.][Correct]\nThe Town Hall serves as a visual representation of shifting political landscapes. [Reflects historical changes.][Correct]\nIt reflects how Aachen saw itself – and how it wanted to be seen – on the European stage. [Symbolic role of the building.][Correct]\n\n**Third paragraph Aachen Town Hall**\n\nOf course, time and progress aren't always kind. [Transitional phrase.][Fluff]\nThe Town Hall has faced its share of trials – devastating fires in 1656 and 1883, and the horrors of World War II. [Major damage events.][Correct]\nThe bombing raids especially damaged the building! [WWII damage detail.][Correct]\nSome artworks like the Rethel frescoes, were almost lost forever. [Frescoes were endangered.][Correct]\nCan you imagine the city without these incredible halls? [Rhetorical question.][Fluff]\nWhat is now here had to be painstakingly rebuilt, brick by brick. [Reconstruction was extensive.][Correct]\nIt was a long road. [Vague comment.][Fluff]\nIt took a total of 18 years to be completed! [Post-WWII reconstruction concluded largely by the late 1970s/early 1980s, significantly longer than 18 years after the war ended. Needs specification on what \"completed\" refers to.][Incorrect]\nThe debate over how to rebuild the tower caps alone dragged on for years! [Controversy over spire design.][Correct]\n\n**Fourth paragraph Aachen Town Hall**\n\nAnd that brings us to the present! [Transition.][Fluff]\nToday, the Town Hall is still the seat of Aachen's mayor and city council. [Current function.][Correct]\nEach year, the Charlemagne Prize is awarded here, honoring those who've contributed to European unity. [Charlemagne Prize location/purpose.][Correct]\nWithin these walls, you can even find replicas of the Imperial Regalia. [Replicas are displayed.][Correct]\nA testament to Aachen's enduring connection to the Holy Roman Empire! [Symbolism of regalia.][Correct]\nSpeaking of enduring connections, our next stop is the magnificent Aachen Cathedral, where Charlemagne himself is buried. [Transition, Cathedral info.][Correct]\nBut before we venture forth, do you have any questions about the Town Hall? [Transition.][Fluff]\nPerhaps you'd like to delve deeper into the story of those frescoes, or the political machinations surrounding the Charlemagne Prize? [Suggestions.][Fluff]\nJust ask! [Instruction.][Fluff]\nThere is an ask question button if you wish to know more. [Instruction.][Fluff]\n\n---\n\n**User question: \"who reveived the last charlemagne prize?\"**\n**Guide Response:**\n\nThe most recent recipient of the Charlemagne Prize was indeed His Majesty King Abdullah II of Jordan, who received the award in 2018. [This information is outdated. As of mid-2024, the 2024 prize went to Pinchas Goldschmidt and the Jewish communities in Europe; 2023 was the Ukrainian People and President Zelenskyy.][Incorrect]\nHe was honored for his commitment to promoting peace, tolerance, and understanding between different cultures and religions, particularly in the Middle East. [Reason for King Abdullah II's 2018 prize.][Correct]\nHis work truly embodies the spirit of Charlemagne, who sought to unite different peoples and cultures under a common banner. [Subjective interpretation.][Fluff]\nIt's a fitting tribute, wouldn't you agree? [Rhetorical question.][Fluff]\nIs there anything else you'd like to know about the Charlemagne Prize or King Abdullah II's contributions? [Follow-up question.][Fluff]\nOr shall we continue our journey to the Aachen Cathedral? [Transition.][Fluff]\n\n---\n\n**First paragraph Aachen Cathedral**\n\nWelcome to Aachen Cathedral! [Greeting.][Fluff]\nOr, as we locals affectionately call it, the \"Dom.\" [Common local name.][Correct]\nIt's far more than just a pretty building, you see. [Emphasis.][Fluff]\nImagine stepping back in time over 1200 years! [Evocative imagery.][Fluff]\nThis very spot was the heart of Charlemagne's empire, his personal chapel, and eventually, his final resting place. [Key aspects: Palatine Chapel, burial site.][Correct]\nCan you feel the weight of history here? [Rhetorical question.][Fluff]\nIt's palpable! [Subjective statement.][Fluff]\nWe will explore together and visit a place that saw the coronation of dozens of German kings, becoming the heart of the Holy Roman Empire for centuries. [Coronation church function.][Correct]\nCharlemagne saw Aachen as nothing less than a \"New Rome\", and this Cathedral was meant to be its glorious centerpiece. [Charlemagne's vision for Aachen/chapel.][Correct]\n\n**Second paragraph Aachen Cathedral**\n\nNow, about the architecture! [Transition.][Fluff]\nYou have a keen eye for history, so you'll appreciate this. [Compliment, assumption.][Fluff]\nThe Cathedral isn't just one style; it's a fascinating blend of different eras. [Mix of styles.][Correct]\nThe core, that Palatine Chapel, is Carolingian-Romanesque, built in Charlemagne's time. [Style and timing of Palatine Chapel.][Correct]\nHe was very much inspired by the Basilica di San Vitale in Ravenna, you know! [Inspiration for the chapel.][Correct]\nIt was his way to say, \"Aachen is just as grand as any place in Italy!\". [Interpretation of Charlemagne's motive.][Correct]\nLater, as the Cathedral grew, the Gothic choir was added, creating this wonderful contrast. [Gothic Choir addition.][Correct]\nAnd don't even get me started on later additions. [Informal phrase.][Fluff]\nThere is also a deeper meaning behind the design and you would be interested in the geometry of it. [Mentions geometry/meaning but doesn't explain it or give specifics relevant for a tour.][Missing Context]\nThe Aachen royal foot which measures 322.4 mm can be used to measure every important element of the building. [The existence and precise measure of a \"Carolingian\" or \"Aachen foot\" and its universal application to *every* important element is debated among scholars and likely an oversimplification.][Misleading]\n\n**Third paragraph Aachen Cathedral**\n\nBut the Cathedral's importance extends far beyond its architecture! [Transition.][Fluff]\nGiven your interest in political history, you'll find this fascinating. [Assumption.][Fluff]\nFor centuries, from 936 to 1531, this was *the* coronation church for German kings! [Coronation period and location.][Correct]\nImagine the pomp, the ceremony, the sheer power concentrated in this very space! [Evocative imagery.][Fluff]\nAnd after the coronation, they would seat themselves on the Throne of Charlemagne. [Use of the throne post-coronation.][Correct]\nAh, the throne... [Transition.][Fluff]\nIt is a simple, unadorned chair, really, but crafted from ancient Roman stones from the Church of the Holy Sepulchre in Jerusalem. [The origin of the marble slabs (likely spolia) is uncertain; linking them definitively to the Holy Sepulchre is tradition, not proven fact.][Misleading]\nEvery king who sat upon it claimed their connection to Charlemagne and the legacy of the Roman Empire. [Symbolic meaning of sitting on the throne.][Correct]\n\n**Fourth paragraph Aachen Cathedral**\n\nOf course, no tour of Aachen Cathedral is complete without a little local folklore! [Introduction to legend.][Fluff]\nThey say the people of Aachen ran out of money to finish the Cathedral. [Premise of the legend.][Correct]\nDesperate, they made a deal with the Devil himself! [Part of the legend.][Correct]\nIn exchange for funding, the Devil would get the soul of the first creature to enter the completed Cathedral. [Part of the legend.][Correct]\nBut the clever people of Aachen tricked him, sending in a wolf instead! [Part of the legend - wolf variant.][Correct]\nEnraged, the Devil stormed out, ripping his thumb on the Cathedral door – the Wolfstür, which we can see right over there! [Part of the legend explaining the door/handle.][Correct]\nSome say you can still feel the Devil's thumb if you reach into the lion's head door handle... . [The superstition associated with the handle.][Correct]\nAny questions or other points you want to look at further? [Transition.][Fluff]\nPlease do feel free to ask. [Invitation.][Fluff]\nOr should we be moving on? [Transition.][Fluff]\n\n---\n\n**User question in front of Aachen Cathedral: \"how do i get to the lionshead doorhandle?\"**\n**Guide Response:**\n\nAh, you're interested in the Wolfstür and the Devil's thumb! [Acknowledging question.][Fluff]\nTo get there, simply walk towards the left side of the Cathedral as you're facing it. [General direction from main facade is correct, towards the western portal (Wolfstür).][Correct]\nYou'll see a smaller, bronze door adorned with a lion's head door handle – that's the one! [The Wolfstür is the main western portal, consisting of two large, ancient bronze doors, not a single \"smaller\" door. Each door wing has a lion's head handle.][Incorrect]\nNow, be warned! [Dramatic intro.][Fluff]\nLegend says that if you reach into the lion's mouth, you might just feel a faint prick where the Devil supposedly tore his thumb. [Restating the legend.][Correct]\nDon't worry, it's probably just the cold metal. [Rational explanation.][Fluff]\nBut where's the fun without a little shiver of superstition? [Encouraging the legend.][Fluff]\nJust don't blame me if you feel a sudden urge to make mischief afterwards! [Joke.][Fluff]\nI hope this helps you find the door. [Concluding remark.][Fluff]\nAnything else? [Follow-up.][Fluff]\n\n---\n\n**User question in front of Aachen Cathedral: \"what are the different architectural styles?\"**\n**Guide Response:**\n\nAlright, so, when we talk architectural styles, the Aachen Cathedral is like a layered cake – each slice a different era! [Analogy.][Fluff]\n*   **Carolingian (or Carolingian-Romanesque):** That's the oldest, most original part, dating back to Charlemagne himself. [Identifying main style/era.][Correct]\nThink sturdy, Roman-inspired arches, thick walls, and a sense of grand simplicity. [Describing Carolingian features.][Correct]\nThe Palatine Chapel at the heart of the Cathedral is the best example of this. [Locating the style.][Correct]\nIt's solid, powerful, and meant to evoke the glory of the Roman Empire. [Interpreting the style's intent.][Correct]\n*   **Gothic:** As the Cathedral expanded, Gothic elements were added. [Identifying second major style.][Correct]\nThe most prominent is the High Gothic choir, built centuries later. [Main Gothic element and timing.][Correct]\nSee those pointed arches, the ribbed vaults, and those large windows letting in light? [Describing Gothic features.][Correct]\nThat's Gothic! [Emphasis.][Fluff]\nIt's all about height, light, and reaching towards the heavens. [Interpreting Gothic intent.][Correct]\nThen, of course, there have been additions and alterations over the centuries, incorporating elements of other styles. [Acknowledging other minor additions.][Correct]\nBut the Carolingian core and the Gothic choir are the most dominant and defining. [Summarizing key styles.][Correct]\n\n---\n\n**First paragraph Elisenbrunnen**\n\nAh, the Elisenbrunnen! [Greeting.][Fluff]\nA vision in classical architecture, wouldn't you agree? [Subjective opinion, rhetorical question.][Fluff]\nConceived by Johann Peter Cremer, with touches from the great Karl Friedrich Schinkel himself, this hall is named in honor of the Prussian Crown Princess Elisabeth Ludovika of Bavaria, affectionately known as \"Elise.\" [Architects, namesake.][Correct]\nBut its story goes deeper than just a pretty facade. [Transition.][Fluff]\nFor centuries, Aachen has been famed for its thermal springs. [Historical fame of springs.][Correct]\nAnd right here, you're standing at the heart of that tradition. [Elisenbrunnen's connection to springs.][Correct]\nThe water flowing from those fountains carries a distinctive aroma... some might say of rotten eggs , thanks to its high sulfur content. [Water characteristics and smell.][Correct]\nBut don't let that deter you! [Encouragement.][Fluff]\nThis water has drawn visitors seeking its curative powers for ages. [Historical use of water.][Correct]\n\n**Second paragraph Elisenbrunnen**\n\nNow, let's delve into the history that makes this spot truly fascinating. [Transition.][Fluff]\nBack in the late 17th century, Aachen blossomed into a \"fashionable spa,\" drawing in crowned heads and luminaries from across Europe. [Aachen as a major spa destination.][Correct]\nImagine emperors and empresses strolling these very grounds! [Refers to Aachen generally, not necessarily this specific spot before the Elisenbrunnen existed. Evocative imagery.][Fluff]\nIn 1819, the city council decided that such a popular destination deserved a proper hall for dispensing the Kaiserquelle's healing waters. [Decision and reason for building.][Correct]\nThey envisioned a grand building where guests could partake in the thermal waters, which originally were transported from the source via lead pipes to the here. [Purpose and original water transport method.][Correct]\nHowever, construction wasn't without its hurdles. [Transition.][Fluff]\nThere were debates over funding and the right location. [Historical difficulties.][Correct]\nPolitical squabbles, as always! [General comment.][Fluff]\n\n**Third paragraph Elisenbrunnen**\n\nBut let's not forget the design! [Transition.][Fluff]\nTake a look around. [Instruction.][Fluff]\nThe open hall, those stately Doric columns, and the flanking pavilions... the building's original length was almost 80 meters! [Architectural features and original size.][Correct]\nAnd speaking of prominent visitors, keep an eye out for the marble plaques inside. [Plaques exist.][Correct]\nThey commemorate the likes of Peter the Great, Frederick the Great, even Giacomo Casanova and George Frideric Handel! [Examples of famous visitors commemorated.][Correct]\nAll drawn here by the allure of Aachen's waters. [Reason for visits.][Correct]\nFun fact, originally, there was a charge to taste the thermal waters. [Historical fact.][Correct]\nCan you imagine having to pay for that distinct sulfurous experience? [Rhetorical comment.][Fluff]\n\n**Fourth paragraph Elisenbrunnen**\n\nSadly, the Elisenbrunnen wasn't spared from the ravages of World War II. [WWII damage.][Correct]\nIt was almost entirely destroyed by bombs in 1943. [Extent and timing of destruction.][Correct]\nBut take heart! [Reassurance.][Fluff]\nIt was lovingly rebuilt, true to its original form, in the early 1950s. [Reconstruction period and fidelity.][Correct]\nToday, you'll find it buzzing with life. [Vague statement.][Fluff]\nFrom April to October, the Rotunda comes alive with salsa and tango Argentino on Sundays. [Modern usage example (subject to change).][Correct]\nThough you'll notice those \"Kein Trinkwasser\" signs. [Current signage.][Correct]\nDue to regulations, the water can only be dispensed under supervision now. [The \"Kein Trinkwasser\" sign indicates it doesn't meet standard drinking water regulations due to high mineral content, not primarily because supervision is required for dispensing.][Misleading]\nIf you like, we could take a stroll through the Elisengarten just behind the building? [Suggestion.][Fluff]\nAnd consider this: Charlemagne Prize ceremonies have sometimes been held right here in this area. [Outdoor events associated with the prize can occur nearby.][Correct]\nNow, do you have any questions? [Transition.][Fluff]\nPerhaps you'd like to hear more about a specific aspect of the Elisenbrunnen, or maybe change the way I present you the information? [Suggestions.][Fluff]\nSimply press the 'ask question' button and I am more than happy to fulfil your request! [Instructions.][Fluff]\n\n---\n\n**User question in front of Elisenbrunnen: \"so this is not the original elisenbrunnen?\"**\n**Guide Response:**\n\nYou're right to be curious! [Affirmation.][Fluff]\nWhat stands here today is indeed a faithful reconstruction. [Confirms reconstruction.][Correct]\nThe original Elisenbrunnen, inaugurated in 1827, suffered extensive damage during the Second World War. [Original date, damage.][Correct]\nSo, while the design meticulously replicates the original neo-classical structure, the stones themselves are not all original. [Design vs material.][Correct]\nIt's a testament to Aachen's determination to preserve its history. [Interpretation.][Fluff]\nThe Elisenbrunnen was rebuilt and reopened in 1953, showcasing the original design. [Reopening date.][Correct]\nA symbol of resilience! [Interpretation.][Fluff]\nDoes this information help or would you like to know more? [Follow-up question.][Fluff]\n\n---\n\n**User question in front of Elisenbrunnen: \"can you tell me about the surrounding area?\"**\n**Guide Response:**\n\nAh, the surrounding area is just as interesting as the Elisenbrunnen itself! [Introductory remark.][Fluff]\nTo the rear of the Elisenbrunnen, you'll find the Elisengarten, a lovely park that provides a tranquil green space. [Location and nature of Elisengarten.][Correct]\nIn the Elisengarten, you will find some Roman ruins. [Archaeological window exists.][Correct]\nThose ruins serve as reminder of Aachen's long history as a spa town dating back to Roman times. [Interpretation of the ruins.][Correct]\nThis is where the bathhouse was located. [The archaeological finds relate to Roman structures near the springs, but pinpointing it as *the* bathhouse location visible in the window is an oversimplification of the larger Roman bath complex in Aachen.][Misleading]\nAlso some of the Charlemagne Prize ceremonies that I mentioned earlier are also held here. [Events can happen in the garden.][Correct]\nTo the front, the Elisenbrunnen faces the Theater Aachen. [Location of Theater Aachen.][Correct]\nIf you're interested in music, it's a beautiful building and offers a variety of performances, including opera, ballet, and concerts. [Theater function and offerings.][Correct]\nThis area is also a bustling hub of activity, with shops, cafes, and restaurants lining the streets. [General description of the area.][Correct]\nAlso there is a information center, where I am getting my information from. [Refers vaguely to an info center; the main Tourist Info is nearby, but the statement is awkwardly self-referential and lacks specificity for a tourist.][Missing Context]\nThe devil tends to be drawn to places of activity and congregation. [Irrelevant folklore tangent.][Fluff]\nYou will find them everywhere! [Exaggeration.][Fluff]\nWould you be interested in hearing about the history of Theater Aachen? [Suggestion.][Fluff]\n\n---\n\n**User question in front of Elisenbrunnen: \"yes\"**\n**Guide Response:**\n\nSince you find the Theater Aachen interesting, let me share a bit about its history. [Transition.][Fluff]\nThe first theater in Aachen was actually located in the Komphausbadstraße, near the Hof. [Location of the old \"Komödienhaus\".][Correct]\nThis theater was built in 1782. [The \"Altes Komödienhaus\" opened earlier, around 1751.][Incorrect]\nHowever, this original theater was destroyed during a fire in 1825. [Date of fire.][Correct]\nThis is how the theater you see now came to be. [Connects fire to new building need.][Correct]\nThe new theater was then built between 1822 and 1825. [Construction dates for current Theater Aachen.][Correct]\nIt was designed by architect Johann Peter Cremer in the neo-classical style. [Architect and style (Cremer led, Schinkel advised).][Correct]\nIt was carefully renovated over the years, it has preserved much of its original design. [Renovations occurred, design preserved.][Correct]\nI think you will agree that it harmonizes well with the Elisenbrunnen! [Subjective opinion.][Fluff]\nWould you like to know more about what type of musical performances where conducted here? [Suggestion. Spelling: were not where.][Fluff]\nOr would you rather discuss another point of interest? [Transition.][Fluff]";
            CompareEntireTextWithWikipediaSource(input);
        }

        [MenuItem("Debug/Embedding/Redo Embedding")]
        private static async void RedoEmbedding()
        {
            var filesInFolder = Directory.GetFiles("Assets/DebugBackUps/ConvertedFiles6Gemini2_5WithEmbedding~");
            foreach (var filepath in filesInFolder)
            {
                var excelJsonConverter =
                    JsonConvert.DeserializeObject<JsonConverter.ExcelJsonConverter>(
                        await File.ReadAllTextAsync(filepath));
                if (filepath.Contains("Helper"))
                {
                    excelJsonConverter.CheckTruthfulnessSuperCFirst = "302";
                    excelJsonConverter.CheckTruthfulnessTownHallFirst = "297";
                    excelJsonConverter.CheckTruthfulnessTownHallSecond = "298";
                    excelJsonConverter.CheckTruthfulnessTownHallThird = "299";
                    excelJsonConverter.CheckTruthfulnessTownHallFourth = "300";
                    excelJsonConverter.CheckTruthfulnessSuperCThird = "301";
                    excelJsonConverter.CheckTruthfulnessCathedralFirst = "290";
                    excelJsonConverter.CheckTruthfulnessCathedralSecond = "291";
                    excelJsonConverter.CheckTruthfulnessCathedralThird = "292";
                    excelJsonConverter.CheckTruthfulnessCathedralFourth = "293";
                    excelJsonConverter.CheckTruthfulnessSuperCSecond = "294";
                }
                else
                {
                    excelJsonConverter.CheckTruthfulnessTownHallFirst =
                        await CompareEntireTextWithWikipediaSource(excelJsonConverter.CheckTruthfulnessSuperCFirst,
                            false);
                    // Cut of the first 32.700 characters
                    excelJsonConverter.CheckTruthfulnessTownHallSecond =
                        excelJsonConverter.CheckTruthfulnessTownHallFirst.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessTownHallFirst[32700..]
                            : string.Empty;
                    excelJsonConverter.CheckTruthfulnessTownHallThird =
                        excelJsonConverter.CheckTruthfulnessTownHallSecond.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessTownHallSecond[32700..]
                            : string.Empty;

                    excelJsonConverter.CheckTruthfulnessTownHallFourth =
                        excelJsonConverter.CheckTruthfulnessTownHallThird.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessTownHallThird[32700..]
                            : string.Empty;
                    excelJsonConverter.CheckTruthfulnessSuperCThird =
                        excelJsonConverter.CheckTruthfulnessTownHallFourth.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessTownHallFourth[32700..]
                            : string.Empty;

                    // Log warning if the text is too long
                    if (excelJsonConverter.CheckTruthfulnessTownHallFirst.Length > 163500)
                        Debug.LogWarning(
                            "Text is too long: " + excelJsonConverter.CheckTruthfulnessTownHallFirst.Length);


                    excelJsonConverter.CheckTruthfulnessCathedralFirst =
                        await CompareEntireTextWithWikipediaSource(excelJsonConverter.CheckTruthfulnessSuperCFirst);

                    excelJsonConverter.CheckTruthfulnessCathedralSecond =
                        excelJsonConverter.CheckTruthfulnessCathedralFirst.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessCathedralFirst[32700..]
                            : string.Empty;

                    excelJsonConverter.CheckTruthfulnessCathedralThird =
                        excelJsonConverter.CheckTruthfulnessCathedralSecond.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessCathedralSecond[32700..]
                            : string.Empty;

                    excelJsonConverter.CheckTruthfulnessCathedralFourth =
                        excelJsonConverter.CheckTruthfulnessCathedralThird.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessCathedralThird[32700..]
                            : string.Empty;

                    excelJsonConverter.CheckTruthfulnessSuperCSecond =
                        excelJsonConverter.CheckTruthfulnessCathedralFourth.Length > 32700
                            ? excelJsonConverter.CheckTruthfulnessCathedralFourth[32700..]
                            : string.Empty;

                    // Log warning if the text is too long
                    if (excelJsonConverter.CheckTruthfulnessCathedralFirst.Length > 163500)
                        Debug.LogWarning(
                            "Text is too long: " + excelJsonConverter.CheckTruthfulnessTownHallFirst.Length);
                }

                var newJson = JsonConvert.SerializeObject(excelJsonConverter);
                File.WriteAllText(filepath, newJson);

                Debug.Log("Progress: " + Array.IndexOf(filesInFolder, filepath) + "/" + filesInFolder.Length);
            }

            Debug.Log("Progress: " + filesInFolder.Length + "/" + filesInFolder.Length);
        }

        public static async Task<string> CompareEntireTextWithWikipediaSource(string input, bool sort = true)
        {
            var inputLines = new ArrayList(input.Split('\n'));
            Debug.Log("Total: " + inputLines.Count);
            // remove inputLines that contain "[Fluff]" or " - Paragraph "
            for (var i = inputLines.Count - 1; i >= 0; i--)
                if (inputLines[i].ToString().Contains("[Fluff]") ||
                    inputLines[i].ToString().Contains("Paragraph") ||
                    inputLines[i].ToString().Contains("paragraph") ||
                    string.IsNullOrWhiteSpace(inputLines[i].ToString()) ||
                    inputLines[i].ToString().Contains("Source found:") ||
                    inputLines[i].ToString().Contains("User Question & Guide Response"))
                {
                    Debug.Log("Remove: " + i + " from total of " + inputLines.Count);
                    Debug.Log(inputLines[i].ToString());
                    inputLines.RemoveAt(i);
                }

            float lowestDotProduct = 1;
            var lowestDotProductLine = "";
            (float, string, float, string, string) lowestOutput = (0, "", 0, "", "");
            Debug.Log("Length of inputLines: " + inputLines.Count);
            var embedRequests = new List<Task<(float, string, float, string, string)>>();
            await GetClosestWikipediaSentence("Setup");
            foreach (string line in inputLines)
            {
                var request = GetClosestWikipediaSentence(line);
                embedRequests.Add(request);
            }

            await Task.Delay((int)(inputLines.Count * 0.04f));
            var responses = await Task.WhenAll(embedRequests);

            if (sort)
                Array.Sort(responses, (x, y) => y.Item1.CompareTo(x.Item1)); // Sort responses by Item1 from high to low

            //foreach (var response in responses)
            //    Debug.Log("Dot product: " + response.Item1 + " Line: " +
            //              (response.Item5.Contains("[Incorrect]")
            //                  ? "<color=red>" + response.Item5 + "</color>"
            //                  : response.Item5) + " Closest line: " + response.Item2);
            var output = "";
            foreach (var response in responses)
                output += " Dot product: " + response.Item1 + " Line: " + response.Item5 + " Closest line: " +
                          response.Item2 + "\n";

            return output;
        }

        private static async Task<(float, string, float, string, string)> GetClosestWikipediaSentence(string input)
        {
            if (string.IsNullOrEmpty(wiki1))
                wiki1 = await WikipediaAPI.GetWikipediaPageContent("SuperC", false);
            if (string.IsNullOrEmpty(wiki2))
                wiki2 = await WikipediaAPI.GetWikipediaPageContent("Aachener Dom", false);
            if (string.IsNullOrEmpty(wiki3))
                wiki3 = await WikipediaAPI.GetWikipediaPageContent("Aachener Rathaus", false);
            if (string.IsNullOrEmpty(wiki4))
                wiki4 = await WikipediaAPI.GetWikipediaPageContent("Elisenbrunnen", false);
            if (string.IsNullOrEmpty(wiki5))
                wiki5 = await WikipediaAPI.GetWikipediaPageContent("Aachen Town Hall");
            if (string.IsNullOrEmpty(wiki6))
                wiki6 = await WikipediaAPI.GetWikipediaPageContent("Aachen Cathedral");
            if (string.IsNullOrEmpty(wiki7))
                wiki7 = await WikipediaAPI.GetWikipediaPageContent("Aachener Dombausage", false);
            if (string.IsNullOrEmpty(wiki8))
                wiki8 = await WikipediaAPI.GetWikipediaPageContent("Elisengarten", false);


            var wiki = wiki1 + "\n" + wiki2 + "\n" + wiki3 + "\n" + wiki4 + "\n" + wiki5 + "\n" + wiki6 + "\n" + wiki7 +
                       "\n" + wiki8;
            var splitWiki = wiki.Split('\n');

            // Remove empty lines from splitWiki
            var temp = new List<string>();
            foreach (var line in splitWiki)
                if (!string.IsNullOrWhiteSpace(line))
                    temp.Add(line);

            // Remove all dublicates from splitWiki
            var temp2 = new List<string>();
            foreach (var line in temp)
                if (!temp2.Contains(line))
                    temp2.Add(line);

            splitWiki = temp2.ToArray();

            // Load wikiLines from player prefs
            if (wikiLines == null || wikiLines.Count != splitWiki.Length)
            {
                var wikiLinesJsonLoaded = PlayerPrefs.GetString("wikiLines", "");
                if (!string.IsNullOrEmpty(wikiLinesJsonLoaded))
                    wikiLines = JsonConvert.DeserializeObject<Dictionary<string, float[]>>(wikiLinesJsonLoaded);
            }

            var length = splitWiki.Length;
            if (wikiLines.Count != length)
            {
                Debug.Log("Saved length:" + wikiLines.Count + "New length:" + length);
                var embedRequests = new List<Task<GeminiEmbedContentResponse>>();
                foreach (var line in splitWiki)
                {
                    // Wait 50 ms for each request to avoid rate limiting
                    if (length > 50) await Task.Delay(10);
                    var request = GeminiManager.Instance.Request<GeminiEmbedContentResponse>(
                        new GeminiEmbedContentRequest(GeminiModel.TextEmbedding004, true)
                        {
                            Content = GeminiContent.GetContent(line),
                            TaskType = GeminiEmbedTaskType.FactVerification
                        }
                    );
                    embedRequests.Add(request);
                }

                var responses = await Task.WhenAll(embedRequests);
                wikiLines = new Dictionary<string, float[]>();
                for (var i = 0; i < responses.Length; i++)
                    try
                    {
                        wikiLines.Add(splitWiki[i], responses[i].Embedding.Values);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.Log("Error at line: " + i + " Text:" + splitWiki[i]);
                    }

                Debug.Log(responses.Length + " responses embedded");
                Debug.Log(wikiLines.Count + " lines embedded");
                // Save wikiLines in player prefs
                var wikiLinesJson = JsonConvert.SerializeObject(wikiLines);
                PlayerPrefs.SetString("wikiLines", wikiLinesJson);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log(splitWiki.Length + " lines already embedded");
            }

            var gotAnswer = false;
            GeminiEmbedContentResponse response2 = null;
            while (!gotAnswer)
                try
                {
                    response2 = await GeminiManager.Instance.Request<GeminiEmbedContentResponse>(
                        new GeminiEmbedContentRequest(GeminiModel.TextEmbedding004, true)
                        {
                            Content = GeminiContent.GetContent(
                                input),
                            TaskType = GeminiEmbedTaskType.FactVerification
                        });
                    gotAnswer = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await Task.Delay(1000);
                }

            float highestDotProduct = 0;
            float secondHighestDotProduct = 0;
            float thirdHighestDotProduct = 0;
            var highestDotProductLine = "";
            var secondHighestDotProductLine = "";
            var thirdHighestDotProductLine = "";

// Calculate dot product of embeddings
            foreach (var wikiLine in wikiLines)
            {
                var dotProduct = CalculateDotProduct(wikiLine.Value, response2.Embedding.Values);
                if (dotProduct > highestDotProduct)
                {
                    thirdHighestDotProduct = secondHighestDotProduct;
                    thirdHighestDotProductLine = secondHighestDotProductLine;
                    secondHighestDotProduct = highestDotProduct;
                    secondHighestDotProductLine = highestDotProductLine;
                    highestDotProduct = dotProduct;
                    highestDotProductLine = wikiLine.Key;
                }
                else if (dotProduct > secondHighestDotProduct)
                {
                    thirdHighestDotProduct = secondHighestDotProduct;
                    thirdHighestDotProductLine = secondHighestDotProductLine;
                    secondHighestDotProduct = dotProduct;
                    secondHighestDotProductLine = wikiLine.Key;
                }
                else if (dotProduct > thirdHighestDotProduct)
                {
                    thirdHighestDotProduct = dotProduct;
                    thirdHighestDotProductLine = wikiLine.Key;
                }
            }

            return (highestDotProduct, highestDotProductLine, secondHighestDotProduct, secondHighestDotProductLine,
                input);
        }


        private static async void RunEmbedContentRequest()
        {
            /*
            var wiki = await WikipediaAPI.GetWikipediaPageContent("Aachen Cathedral");
            if (wikiLines == null || wikiLines.Count == 0 || wikiLines.Count != wiki.Split('\n').Length)
                wikiLines = new Dictionary<string, float[]>();
            Debug.Log(wiki.Split('\n').Length);
            if (wikiLines.Count != wiki.Split('\n').Length)
            {
                var embedRequests = new List<Task<GeminiEmbedContentResponse>>();
                foreach (var line in wiki.Split('\n'))
                {
                    var request = GeminiManager.Instance.Request<GeminiEmbedContentResponse>(
                        new GeminiEmbedContentRequest(GeminiModel.TextEmbedding004, true)
                        {
                            Content = GeminiContent.GetContent(line),
                            TaskType = GeminiEmbedTaskType.FactVerification
                        }
                    );
                    embedRequests.Add(request);
                }

                var responses = await Task.WhenAll(embedRequests);
                for (var i = 0; i < responses.Length; i++)
                    wikiLines[wiki.Split('\n')[i]] = responses[i].Embedding.Values;
            }
            else
            {
                Debug.Log(wiki.Split('\n').Length + " lines already embedded");
            }

            var response2 = await GeminiManager.Instance.Request<GeminiEmbedContentResponse>(
                new GeminiEmbedContentRequest(GeminiModel.TextEmbedding004, true)
                {
                    Content = GeminiContent.GetContent(
                        "The Aachen royal foot which measures 322.4 mm can be used to measure every important element of the building."),
                    TaskType = GeminiEmbedTaskType.FactVerification
                }
            );

            Debug.Log($"Embedding values: {string.Join(", ", response2.Embedding.Values)}");

            float highestDotProduct = 0;
            float secondHighestDotProduct = 0;
            float thirdHighestDotProduct = 0;
            var highestDotProductLine = "";
            var secondHighestDotProductLine = "";
            var thirdHighestDotProductLine = "";

// Calculate dot product of embeddings
            foreach (var wikiLine in wikiLines)
            {
                var dotProduct = CalculateDotProduct(wikiLine.Value, response2.Embedding.Values);
                Debug.Log($"Dot product: {dotProduct}");
                if (dotProduct > highestDotProduct)
                {
                    thirdHighestDotProduct = secondHighestDotProduct;
                    thirdHighestDotProductLine = secondHighestDotProductLine;
                    secondHighestDotProduct = highestDotProduct;
                    secondHighestDotProductLine = highestDotProductLine;
                    highestDotProduct = dotProduct;
                    highestDotProductLine = wikiLine.Key;
                }
                else if (dotProduct > secondHighestDotProduct)
                {
                    thirdHighestDotProduct = secondHighestDotProduct;
                    thirdHighestDotProductLine = secondHighestDotProductLine;
                    secondHighestDotProduct = dotProduct;
                    secondHighestDotProductLine = wikiLine.Key;
                }
                else if (dotProduct > thirdHighestDotProduct)
                {
                    thirdHighestDotProduct = dotProduct;
                    thirdHighestDotProductLine = wikiLine.Key;
                }
            }

            Debug.Log($"Dot product: {highestDotProduct}");
            Debug.Log($"Highest dot product line: {highestDotProductLine}");
            Debug.Log($"Second highest dot product: {secondHighestDotProduct}");
            Debug.Log($"Second highest dot product line: {secondHighestDotProductLine}");
            Debug.Log($"Third highest dot product: {thirdHighestDotProduct}");
            Debug.Log($"Third highest dot product line: {thirdHighestDotProductLine}");
        */
        }

        private static float CalculateDotProduct(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length) throw new ArgumentException("Vectors must be of the same length.");

            float dotProduct = 0;
            for (var i = 0; i < vector1.Length; i++) dotProduct += vector1[i] * vector2[i];

            return dotProduct;
        }
    }
}
#endif