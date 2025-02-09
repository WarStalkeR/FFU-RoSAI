using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_Credits : ToggleableWindowController, IInputActionHandler
    {
        public static Window_Credits Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_Credits()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }
        
        public readonly static KeyValuePair<EntryType, string>[] Entries = new KeyValuePair<EntryType, string>[]
        {
            new KeyValuePair<EntryType, string>( EntryType.Header1, "Arcen Games – Development"),

            #region Phase Three
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Phase Three"),
            new KeyValuePair<EntryType, string>( EntryType.Header3, "May 2024 Onward"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "\"Solo\" Developer - Programming, Writing, Design, Etc."),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Chris McElligott Park"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Music"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Pablo Vega"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "UI/UX Redesign And Additional Art"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Josh Atkinson (Hooded Horse)"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Scenario Design - Very Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kara McElligott Park, MD, MPH, MMCI"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Ongoing Key Design Advisor - Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Abhishek Chaudhry (Hooded Horse)"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Player Experience Key Advisor - Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mandalore Herrington (Hooded Horse)"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Additional Writing And Design"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Rosalind Parfrey"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Key Tester And Design Advice"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mateusz Błażewicz (QLOC)"),
            #endregion

            #region Phase Two
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Phase Two"),
            new KeyValuePair<EntryType, string>( EntryType.Header3, "June 2023 To April 2024"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Ground-Up Rework As Solo Developer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Chris McElligott Park"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Sound Design"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Chris McElligott Park"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Key Design Advisor - The Most Special Of Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Abhishek Chaudhry (Hooded Horse)"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Background Traffic Programming - Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Major Johnson"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Religion Designer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "George Tibbitts"),
            #endregion

            #region Phase One
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Phase One"),
            new KeyValuePair<EntryType, string>( EntryType.Header3, "June 2022 To May 2023"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Producer, Lead Programmer, Lead Designer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Chris McElligott Park"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Senior Programmer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Willard Davis"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Major Johnson"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Programmer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Spencer Davis"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Junior Programmer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Daniel Garcevic"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Environmental Lead"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Jeff Shinkle"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Gameplay Designer And Lead Writer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "George Tibbitts"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Writer And Worldbuilding"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Elizabeth Hodgson"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Pipeline Integration Specialist and Junior Designer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Marcello Perricone"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Arcenverse Loremaster And Additional Design"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ethan Wong"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Tech Tree Consulting"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Strategic Sage"),
            #endregion
            
            #region Preproduction
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Preproduction"),
            new KeyValuePair<EntryType, string>( EntryType.Header3, "December 2021 To May 2022"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Chris McElligott Park"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Willard Davis"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Tim Bender (Hooded Horse)"),
            #endregion

            #region Special Thanks
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kara McElligott Park"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Christopher and Yvonne"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "William C. Taylor"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Margaret and Michael Park"),

            #endregion
            
            new KeyValuePair<EntryType, string>( EntryType.Header1, "Hooded Horse – Publishing"),

            #region Hooded Horse Main
            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Chief Executive Officer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Tim Bender"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "President & Chief Financial Officer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Snow Rui"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Chief Product Officer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ashkan Namousi"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Chief Commercial Officer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "James Gardiner"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Chief Marketing Officer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Abhishek Chaudhry"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Chairman of the Board"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Shams Jorjani"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of Player Experience"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mandalore Herrington"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of Communications"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Joe Robinson"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of Creator Relations"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Antony Floyd aka HForHavoc"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of Marketing Coordination"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Matthijs Hoving"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of Community"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Matthew Palacio"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of IT & Systems"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Patrik Arvidsson"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Publishing Producer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Dastan Namousi"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Tom King"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Localization Producer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Michael Radnitz"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Localization Coordinator"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Miguel David Medina García"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Lead Release Manager (Primary)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Natalia Montero"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Senior Release Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Blossom Kremer"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Creator Relations Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Renee April"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Strategic Partner Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ga-Yi Ng"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Marketing Associate (Japan, Korea & Southeast Asia)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "LamNot"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Special Projects Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Marcello Perricone"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Concept Artist"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Daniel Morawski"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Sarah Mills"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Videographer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kacper Bork"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Accountant"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Meridith Malone"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Office Administrator"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bailey Sullivan"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Marketing Consultant"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "PartyElite"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Special Thanks"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Aaron Nathan"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Andrew Hume"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Grzegorz Styczeń"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Martti Aarnio-Wihuri"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Todd D’Arcy"),
            #endregion Hooded Horse Main
            
            #region Trailer Production
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Trailer Production"),
            new KeyValuePair<EntryType, string>( EntryType.Header3, "Slavic Magic"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Grzegorz Styczeń"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Patrycja Gdula"),
            #endregion

            #region PR
            new KeyValuePair<EntryType, string>( EntryType.Header2, "PR"),
            new KeyValuePair<EntryType, string>( EntryType.Header3, "GameDash"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bing Li"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Tian Luo"),
            #endregion

            #region Localization
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Localization"),

            new KeyValuePair<EntryType, string>( EntryType.Header3, "Hooded Horse"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Brazilian Portuguese - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Brenda Triandafeledis"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lucas Pereira"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Japanese - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Moeko Sakishita"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Spanish - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Laura Nieves Díaz Suárez"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Marta Medina González"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Miguel David Medina García"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Luis Álvarez Dato"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Traditional Chinese - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Guo Yu Shun Wilson"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Melody Lin"),

            new KeyValuePair<EntryType, string>( EntryType.Header3, "ECI Games"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Project Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ángel Hernández Santana"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kasia Kępka"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mads Johannes Nielsen"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Zhao Ting"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Zhenchuan Luo"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "French - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Caroline Tsakanias"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lueje Prigent"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Marine Wong-Tze-Kioon"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Maxime Aduriz"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "French - LQA"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Caroline Tsakanias"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Félix Braconnier"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "German - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Nino Kadletz"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Philipp Kolleritsch"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Martin Monsberger"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "German - LQA"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Philipp Otschonovsky"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Japanese - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Takashi Hashizume"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mina Horiba-Maguire"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Naomi Ueda"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Keigo Yonemura"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Japanese - LQA"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Noriki Tanegashima"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "H.D."),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Korean - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Changhee An"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Inha Cho "),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Wonder Shin"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Eunnara Cho"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Korean - LQA"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Sue Park"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Simplified Chinese - Localization"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Swain"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Pu Jing"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Huang Xin"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Wu Fan"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Sherry Wang"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Wang Xueyan"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Simplified Chinese - LQA"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Michael Chu"),
            #endregion

            #region QA
            new KeyValuePair<EntryType, string>( EntryType.Header2, "Quality Assurance"),

            new KeyValuePair<EntryType, string>( EntryType.Header3, "QLOC S.A."),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "General Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Adam Piesiak"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Business Development Director"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Elena Roor"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Senior Business Development Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Adrian Czerwiński"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Director of Quality Assurance"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Sergiusz Ślosarczyk"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "QA Operations Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Paweł Strzelczyk"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Lead QA Project Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Jakub Dudkowski"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "QA Project Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Karolina Jagnyziak"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "QA Lab Managers"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Piotr Badurek"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Emil Lubowicki"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Gabriela Malinowska"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Tomasz Prusicki"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Learning & Development Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Michał Raczyński"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Learning & Development Coordinator"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Aleksandra Grigorian-Kopka"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Learning & Development Specialists"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Damian Jasiński"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "QA Team Leader"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Konrad Kaczmarzyk"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Jan Kuzior"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "QA Testers"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mateusz Błażewicz"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Daniel Lipiec"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Maciej Piernicki"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Maksymilian Skuza"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "IT Director"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Artur Szczurzyński"),

            new KeyValuePair<EntryType, string>( EntryType.Header3, "Testronic"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Compliance and Compatibility Manager"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Iwona Szarzyńska"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Senior Compatibility Project Lead"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Małgorzata Twarogal"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Associate Compatibility QA Lead"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Michał Soral"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Compatibility QA Technicians"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Dominik Krawczyk"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Haran Dev Murugan"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bartłomiej \"Akur\" Stajkowski"),
            #endregion
            
            new KeyValuePair<EntryType, string>( EntryType.Header1, "Playtesters (Arcen)"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Top Contributors - Also Keeping Chris On Task"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Andyman119"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Fluffiest"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kenken244"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lukas"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mintdragon"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "New and Tasty"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Pingcode"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "ptarth"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Waladil"),

            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Major Contributors"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Abhishek Chaudhry (Hooded Horse)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Alias50"), //stuffy
            new KeyValuePair<EntryType, string>( EntryType.Names, "arlen_tektolnes"), //Jake Cooper
            new KeyValuePair<EntryType, string>( EntryType.Names, "b0884"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Blackclaws"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bobtree"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Cblln"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Darloth"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Evil Bistro"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Felix Winterhalter"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Gloraion"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Hawk_v3"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "IrlMorgana"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Irl_VriskaSerket"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "jonathansfox"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kane York"), //riking28
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lailah"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Leximancer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lordsamuel"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lord Of Nothing"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Lord Trogg"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mac"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Marcelo Mattioli"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mandalore Herrington (Hooded Horse)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "MOREDAKKA"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Paladin852"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "StormyEyed"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Strategic Sage"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "strandofgibraltar"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "swizzlewizzle"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "The Grand Mugwump"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Wolfier"),
            
            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Contributors"),
            new KeyValuePair<EntryType, string>( EntryType.Names, ":[þs\\)\\?]"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "03noster"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "AC"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "A Devil Chicken"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "ainz ooal gown"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Amon'kurath"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ant"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Antony Floyd aka HForHavoc (Hooded Horse)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Aljon Broekman"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Andrew Mayo"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Arcanestomper"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Aracy"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "ArnaudB"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Arphar"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Athena"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "AtlasMKII"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "AviKav"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bathtub"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "bearforceone"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bleh"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "blue_kit_red"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Bossman"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "c67f"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Cel"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Cimbri"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Choi"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Cortexion"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "crazykid080"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Creakit"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "crossbowman5"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "CRCGamer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Cyborg"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Dad's Gaming Addiction"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "DasBreitschwert"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Destructomonkey"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "donblas"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Dr.Cthulhu"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "dragorislordofmercy"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "EagleShark"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Eggland's Worst"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Esoop"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "FroGG2"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Gabarn"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "George Tibbitts"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "GC13"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "glencoe2004"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "golsutangen"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Hasrem"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Half Phased"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Igor Savin"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "iacore"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Isith"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ithuriel"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Jacek Mańko"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Jack Trades"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Justin Vannerson"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "KillerFrogs"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Kira Mahoni"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Liam"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "LilLillyFox"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Levily"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Leland Creswell"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "mahu"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Major Johnson"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Matt Hoving (Hooded Horse)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Miikka Ryökäs"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Michael Dunkel"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mikeyd"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Mike Dull"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "MikeKimPiggy"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "mercuryminded"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Monkooky"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "NihilRex"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ninetailed"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Norax"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Not The Real Obama"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Pizza"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "pizzapope"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Puffin"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Qwertyfizz"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "relmz32"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Reyouka"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Rebecca Labfiend"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Rhiwaow"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Rhomplestomper"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Rhys H"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Rob (eXplorminate)"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "RED"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "ssamgang 2"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "SciencePenguin"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Scott"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "ScrObot"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Shadow"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Shalax"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Shimaaji"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "shoe_bert"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Slayer"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "skanx"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Solar Sausage"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Space"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Spelguru"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Stetc"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Synthetic"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Thatboi193"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "That one Weeb"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "The Gerbilest"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "thelonebadger"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Tiberiumkyle"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "teo4512"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Techbane"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ubercharge"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Ultrapotassium"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Usama"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Valectar"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "veevoir"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Void00"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Vinco"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "WhereAmI"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "WingedKagouti"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Wolfone"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Wylker"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Yap"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Yog-Sothoth"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Yonder"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Zanthra"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Zeal"),
            new KeyValuePair<EntryType, string>( EntryType.Names, "Zeratul"),

            new KeyValuePair<EntryType, string>( EntryType.Header1, "Thank You For Playing!"),
            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "\"Solo\" Development Refers To Only One Person"),
            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "Touching All The Code And Writing All The Words"),
            new KeyValuePair<EntryType, string>( EntryType.JobTitle, "But The Truth Is It Always Takes A Village"),
        }; 

        public static RectTransform SizingRect;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_Credits.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static TextAbstractBase.TextPool<tHeader1> tHeader1Pool;
            private static TextAbstractBase.TextPool<tHeader2> tHeader2Pool;
            private static TextAbstractBase.TextPool<tHeader3> tHeader3Pool;
            private static TextAbstractBase.TextPool<tJobTitle> tJobTitlePool;
            private static TextAbstractBase.TextPool<tNames> tNamesPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( tHeader1.Original != null )
                        {
                            hasGlobalInitialized = true;
                            tHeader1Pool = new TextAbstractBase.TextPool<tHeader1>( tHeader1.Original, 10, "tHeader1" );
                            tHeader2Pool = new TextAbstractBase.TextPool<tHeader2>( tHeader2.Original, 10, "tHeader2" );
                            tHeader3Pool = new TextAbstractBase.TextPool<tHeader3>( tHeader3.Original, 10, "tHeader3" );
                            tJobTitlePool = new TextAbstractBase.TextPool<tJobTitle>( tJobTitle.Original, 10, "tJobTitle" );
                            tNamesPool = new TextAbstractBase.TextPool<tNames>( tNames.Original, 10, "tNames" );
                        }
                    }
                    #endregion

                    tHeader1Pool.Clear( 60 );
                    tHeader2Pool.Clear( 60 );
                    tHeader3Pool.Clear( 60 );
                    tJobTitlePool.Clear( 60 );
                    tNamesPool.Clear( 60 );

                    RectTransform rTran = (RectTransform)tHeader1.Original.Element.RelevantRect.parent;

                    maxYToShow = -rTran.anchoredPosition.y;
                    minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                    maxYToShow += EXTRA_BUFFER;

                    runningY = topBuffer;

                    this.OnUpdate_Content();

                    #region Positioning Logic
                    //Now size the parent, called Content, to get scrollbars to appear if needed.
                    Vector2 sizeDelta = rTran.sizeDelta;
                    sizeDelta.y = MathA.Abs( runningY );
                    rTran.sizeDelta = sizeDelta;
                    #endregion

                    SizingRect = this.Element.RelevantRect;
                }
            }

            private float maxYToShow;
            private float minYToShow;
            private float runningY;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 400; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load
            private const float MAIN_CONTENT_WIDTH = 800f;

            #region CalculateBoundsForType
            protected float leftBuffer = 5.1f;
            protected float topBuffer = -6;

            protected void CalculateBoundsForType( bool isFirst, EntryType Type, out Rect soleBounds, ref float innerY )
            {
                float rowHeight = 0;
                float spaceAfter = 0f;
                switch ( Type )
                {
                    case EntryType.Header1:
                        if ( !isFirst )
                            innerY -= 30f; //space before!
                        rowHeight = 36f;
                        spaceAfter = 6f;
                        break;
                    case EntryType.Header2:
                        innerY -= 20f; //space before!
                        rowHeight = 30f;
                        spaceAfter = 6f;
                        break;
                    case EntryType.Header3:
                        rowHeight = 26f;
                        spaceAfter = 6f;
                        break;
                    case EntryType.JobTitle:
                        rowHeight = 18f;
                        spaceAfter = 3f;
                        break;
                    case EntryType.Names:
                        rowHeight = 18f;
                        spaceAfter = 0.1f;
                        break;
                }

                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, rowHeight );

                innerY -= rowHeight + spaceAfter;
            }
            #endregion

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                bool wasName = false;
                bool isFirst = true;
                foreach ( KeyValuePair<EntryType, string> kv in Entries )
                {
                    if ( wasName && kv.Key != EntryType.Names )
                    {
                        wasName = false;
                        if ( kv.Key == EntryType.Header3 )
                            runningY -= 32f;
                        else
                            runningY -= 12f;
                    }
                    else if ( !wasName && kv.Key == EntryType.Names )
                        runningY += 3f;

                    this.CalculateBoundsForType( isFirst, kv.Key, out Rect bounds, ref runningY );
                    isFirst = false;

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    switch ( kv.Key )
                    {
                        case EntryType.Header1:
                            {
                                tHeader1 row = tHeader1Pool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( row == null )
                                    continue;
                                tHeader1Pool.ApplySingleItemInRow( row, bounds, false );
                                row.Assign( kv.Value );
                            }
                            break;
                        case EntryType.Header2:
                            {
                                tHeader2 row = tHeader2Pool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( row == null )
                                    continue;
                                tHeader2Pool.ApplySingleItemInRow( row, bounds, false );
                                row.Assign( kv.Value );
                            }
                            break;
                        case EntryType.Header3:
                            {
                                tHeader3 row = tHeader3Pool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( row == null )
                                    continue;
                                tHeader3Pool.ApplySingleItemInRow( row, bounds, false );
                                row.Assign( kv.Value );
                            }
                            break;
                        case EntryType.JobTitle:
                            {
                                tJobTitle row = tJobTitlePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( row == null )
                                    continue;
                                tJobTitlePool.ApplySingleItemInRow( row, bounds, false );
                                row.Assign( kv.Value );
                            }
                            break;
                        case EntryType.Names:
                            {
                                tNames row = tNamesPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( row == null )
                                    continue;
                                tNamesPool.ApplySingleItemInRow( row, bounds, false );
                                row.Assign( kv.Value );
                                wasName = true;
                            }
                            break;
                    }
                }

                runningY -= 60f;
            }
            #endregion
        }

        //not actually needed at this time, but needed for compilation
        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        #region tHeader1
        public class tHeader1 : TextAbstractBase
        {
            public static tHeader1 Original;
            public tHeader1() { if ( Original == null ) Original = this; }

            private string lastText = null;

            public void Assign( string Text )
            {
                if ( this.lastText != Text )
                {
                    ArcenUIWrapperedTMProText text = (this.Element as ArcenUI_Text).Text;
                    text.DirectlySetNextText( Text );
                    text.SetTextNowIfNeeded( true, true );
                    this.lastText = Text;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Text, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( this.lastText );
            }

            public override void Clear()
            {
                this.lastText = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.lastText == null;
            }
        }
        #endregion

        #region tHeader2
        public class tHeader2 : TextAbstractBase
        {
            public static tHeader2 Original;
            public tHeader2() { if ( Original == null ) Original = this; }

            private string lastText = null;

            public void Assign( string Text )
            {
                if ( this.lastText != Text )
                {
                    ArcenUIWrapperedTMProText text = (this.Element as ArcenUI_Text).Text;
                    text.DirectlySetNextText( Text );
                    text.SetTextNowIfNeeded( true, true );
                    this.lastText = Text;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Text, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( this.lastText );
            }

            public override void Clear()
            {
                this.lastText = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.lastText == null;
            }
        }
        #endregion

        #region tHeader3
        public class tHeader3 : TextAbstractBase
        {
            public static tHeader3 Original;
            public tHeader3() { if ( Original == null ) Original = this; }

            private string lastText = null;

            public void Assign( string Text )
            {
                if ( this.lastText != Text )
                {
                    ArcenUIWrapperedTMProText text = (this.Element as ArcenUI_Text).Text;
                    text.DirectlySetNextText( Text );
                    text.SetTextNowIfNeeded( true, true );
                    this.lastText = Text;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Text, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( this.lastText );
            }

            public override void Clear()
            {
                this.lastText = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.lastText == null;
            }
        }
        #endregion

        #region tJobTitle
        public class tJobTitle : TextAbstractBase
        {
            public static tJobTitle Original;
            public tJobTitle() { if ( Original == null ) Original = this; }

            private string lastText = null;

            public void Assign( string Text )
            {
                if ( this.lastText != Text )
                {
                    ArcenUIWrapperedTMProText text = (this.Element as ArcenUI_Text).Text;
                    text.DirectlySetNextText( Text );
                    text.SetTextNowIfNeeded( true, true );
                    this.lastText = Text;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Text, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( this.lastText );
            }

            public override void Clear()
            {
                this.lastText = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.lastText == null;
            }
        }
        #endregion

        #region tNames
        public class tNames : TextAbstractBase
        {
            public static tNames Original;
            public tNames() { if ( Original == null ) Original = this; }

            private string lastText = null;

            public void Assign( string Text )
            {
                if ( this.lastText != Text )
                {
                    ArcenUIWrapperedTMProText text = (this.Element as ArcenUI_Text).Text;
                    text.DirectlySetNextText( Text );
                    text.SetTextNowIfNeeded( true, true );
                    this.lastText = Text;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Text, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( this.lastText );
            }

            public override void Clear()
            {
                this.lastText = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.lastText == null;
            }
        }
        #endregion

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        public void Handle( int Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    this.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }

        public enum EntryType
        {
            Header1,
            Header2, 
            Header3, 
            JobTitle,
            Names
        }
    }
}
