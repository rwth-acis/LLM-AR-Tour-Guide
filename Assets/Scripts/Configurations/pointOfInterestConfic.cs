using System;
using System.Collections.Generic;
using i5.LLM_AR_Tourguide.TourGeneration;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.Configurations
{
    [CreateAssetMenu(fileName = "PointOfInterestData", menuName = "TourGuide/POI Data")]
    public class PointOfInterestData : ScriptableObject
    {
        [SerializeField] public POI[] pointOfInterests =
        {
            new()
            {
                title = "SuperC", subtitle = "A modern student service center",
                information =
                    "From the grandeur of the Town Hall, our tour takes a leap into the 21st century as we arrive at SuperC, the ultra-modern student service center of RWTH Aachen University. Opened in 2008, SuperC is a striking example of contemporary architecture and a symbol of Aachen's dynamism as a university town.\r\nSuperC's design is both innovative and functional. The building's cantilevered top floor provides a large meeting and conference area, while a spacious multi-functional hall beneath the entry square hosts exhibitions and events. The transparent south fa�ade offers glimpses into the building's interior, creating a sense of openness and accessibility. Interestingly, there's an anecdote that the top floor was initially planned as a library, but due to weight considerations, it was repurposed as conference rooms.\r\nBut SuperC is not just about modern aesthetics; it also embraces sustainability. The building is heated and cooled by a deep borehole heat exchanger, making it a pioneer in geothermal energy use in an inner-city location. This commitment to environmental responsibility reflects Aachen's forward-thinking approach and its dedication to creating a sustainable future. The project was funded as an EU demonstration project and received generous donations from various companies, associations, and individuals.\r\nAs we explore SuperC's vibrant spaces, we can feel the energy and creativity of Aachen's student community. This building is a hub of activity, a place where students from all over the world come together to learn, collaborate, and shape the future. SuperC stands as a testament to Aachen's role as a leading center of science and technology in the 21st century. RWTH Aachen University itself is one of Europe's leading research centers, contributing significantly to the city's modern identity.",
                coordinates = new Coordinates
                {
                    latitude = 50.7782863556824, longitude = 6.078553073240601, altitude = 230, metersOverGround = 10
                },
                subPointOfInterests = new List<SubPointOfInterest>
                {
                    new()
                    {
                        title = "RWTH Hauptgebaude",
                        description =
                            "The RWTH Hauptgebaude is the main building of RWTH Aachen University. The building houses various faculties, lecture halls, and administrative offices, serving as the central hub of the university.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77769215370003, longitude = 6.0778240750238925, altitude = 230,
                            metersOverGround = 10
                        },
                        sourceTitle = "Hauptgebäude der RWTH Aachen"
                    },
                    new()
                    {
                        title = "RWTH Central Library",
                        description =
                            "The RWTH Central Library, located at Templergraben 61 in Aachen, is the primary resource for literature and information for RWTH Aachen University, supporting teaching, research, and study. It houses over 1.1 million volumes, including specialized collections in humanities, social sciences, and economics, as well as a comprehensive collection of scientific and technical literature. The library also features the Patent and Standards Center, an information center, and 550 study spaces across its two buildings.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77878130559004, longitude = 6.080227914696778, altitude = 230,
                            metersOverGround = 10
                        },
                        sourceTitle = "Universitätsbibliothek der RWTH Aachen"
                    }
                }
            },
            new()
            {
                title = "Aachen Town Hall", subtitle = "A Gothic architectural masterpiece",
                information =
                    "Our journey through time continues as we arrive at the Aachen Town Hall, a Gothic masterpiece that stands opposite the cathedral. Built in the first half of the 14th century, the town hall was a symbol of Aachen's civic freedom. However, it also played a crucial role in the imperial ceremonies, hosting the traditional coronation feast for the newly crowned Holy Roman Emperors.\r\nThe town hall's history is as rich and layered as its architecture. Constructed on the foundations of Charlemagne's palace, the building incorporates elements from different eras. The Granus Tower, dating back to Charlemagne's time, is integrated into the south side of the building. Over the centuries, the town hall has undergone various transformations, with Baroque elements added after the Great Fire of Aachen in 1656. In the 19th century, the town hall was rebuilt in a neo-Gothic style, preserving its original Gothic elements while adding new features.\r\nToday, the Aachen Town Hall is not only a seat of local government but also a cultural treasure. It houses two museums: the International Newspaper Museum, with its vast collection of newspapers from around the world, and a museum dedicated to Charlemagne and his era. As we admire the town hall's imposing fa�ade, adorned with statues of 50 kings and symbols of art, science, and Christianity, we are reminded of Aachen's long and illustrious history as a center of power, culture, and civic pride.",
                coordinates = new Coordinates
                {
                    latitude = 50.776063722350706, longitude = 6.083747133750778, altitude = 230, metersOverGround = 10
                },
                subPointOfInterests = new List<SubPointOfInterest>
                {
                    new()
                    {
                        title = "Granus Tower",
                        description =
                            "The Granus Tower is an ancient tower integrated into the Aachen Town Hall. Dating back to Charlemagne's time, the tower is a remnant of the emperor's palace and a symbol of Aachen's rich history.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77603821035848, longitude = 6.084162642308442, altitude = 230,
                            metersOverGround = 10
                        },
                        sourceTitle = "Granusturm"
                    },
                    new()
                    {
                        title = "Karlsbrunnen",
                        description =
                            "The Karlsbrunnen is a fountain located in front of the Aachen Town Hall. The fountain features a statue of Charlemagne, the city's most famous historical figure.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77642889885434, longitude = 6.083669633927191, altitude = 230,
                            metersOverGround = 2
                        },
                        sourceTitle = "Karlsbrunnen (Aachen)"
                    }
                }
            },
            new()
            {
                title = "Aachen Cathedral", subtitle = "A UNESCO World Heritage Site",
                information =
                    "From the Elisenbrunnen, we make our way to the magnificent Aachen Cathedral, a UNESCO World Heritage Site and a true masterpiece of architectural and religious significance. Commissioned by Charlemagne himself, the cathedral's construction began around 796 AD, with the towers of his Palatine Chapel dating from this time. The cathedral's unique design, blending Carolingian, Romanesque, and Gothic elements, has had a profound influence on German church architecture.\r\nAachen Cathedral is not only an architectural marvel but also a place of deep historical and spiritual importance. Charlemagne, who made Aachen the center of his empire, was buried in the cathedral in 814 AD. For centuries, the cathedral served as the coronation church for German kings and queens, with 31 emperors being crowned within its hallowed halls. In 1978, Aachen Cathedral became the first German cultural monument to be designated a UNESCO World Heritage Site, a testament to its global significance. The cathedral's treasury houses an invaluable collection of medieval art objects, including Charlemagne's Throne, a golden altarpiece, and a golden pulpit.\r\nAs we stand in the cathedral's octagonal core, beneath its soaring dome, we are transported back in time. We can almost imagine the grandeur of Charlemagne's court, the solemn ceremonies of imperial coronations, and the devotion of countless pilgrims who have come to this sacred place over the centuries.\r\nOne of the most intriguing stories associated with the cathedral is the legend of the \"Wolf Door.\" According to this legend, the citizens of Aachen made a deal with the devil to finance the cathedral's construction. In return, the devil demanded the first soul to enter the cathedral upon its completion. To outsmart the devil, the citizens released a wolf into the cathedral. Enraged, the devil slammed the door shut, trapping his thumb in the process. To this day, visitors can see a bump in the lion's head door knocker, said to be the devil's thumb.\r\nThe Aachen Cathedral is a testament to the enduring power of faith, history, and human ingenuity. It stands as a symbol of Aachen's past glory and its continued importance as a spiritual and cultural center.",
                coordinates = new Coordinates
                {
                    latitude = 50.77473825285982, longitude = 6.0838824869103965, altitude = 230, metersOverGround = 10
                },
                subPointOfInterests = new List<SubPointOfInterest>
                {
                    new()
                    {
                        title = "Wolf Door",
                        description =
                            "The Wolf Door is a legendary entrance to the Aachen Cathedral. According to the legend, the citizens of Aachen outsmarted the devil by releasing a wolf into the cathedral, trapping the devil's thumb in the door knocker.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77474278193228, longitude = 6.083433585107797, altitude = 230,
                            metersOverGround = 1
                        },
                        sourceTitle = "Aachener Dombausage"
                    },
                    new()
                    {
                        title = "Aachen Cathedral Treasury Museum",
                        description =
                            "The treasures reflect almost seamlessly the art periods from the early Middle Ages to modern times. Their significance is closely linked to the purpose they originally served: the furnishing of St. Mary's Church, founded by Charlemagne, and the celebration of its liturgy. In the Cathedral Treasury, 130 extraordinary works of art are exhibited on three floors, telling the story of the 1200-year history of the Aachen Cathedral Treasury.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.775046530063115, longitude = 6.0827136351518325, altitude = 230,
                            metersOverGround = 1
                        },
                        sourceTitle = "Aachener Domschatzkammer"
                    }
                }
            },
            new()
            {
                title = "Elisenbrunnen", subtitle = "A neoclassical spa pavilion",
                information =
                    "Our next stop takes us to the Elisenbrunnen, a neoclassical pavilion that embodies Aachen's spa town heritage. Completed in 1827, the Elisenbrunnen is named after Elisabeth Ludovika von Bayern, who later became Queen Consort of Prussia. She was a frequent visitor to Aachen's healing springs, which have been attracting people to the city since Roman times. In fact, it is believed that Emperor Charlemagne chose to make Aachen his capital primarily because of the beneficial effects of these naturally heated thermal springs.\r\nThe Elisenbrunnen stands as a symbol of Aachen's enduring connection to its natural resources. The pavilion houses two thermal springs, known for their healing properties, which are directly connected to the city's hot springs some three kilometers away. The architecture of the Elisenbrunnen is a beautiful example of neoclassical design. The pavilion consists of two halls connected by a central rotunda adorned with Corinthian columns. An entablature above the columns bears the Latin inscription \"Aquisgranum Fontibus Orta Salus,\" meaning \"Health has its source in Aachen's springs.\" Inside one of the halls, two drinking fountains dispense hot (74�C) sulfurous water, which, despite its distinctive smell, was once believed to have healing properties.\r\nThe Elisenbrunnen is a popular meeting place for locals and tourists alike, a place to relax and enjoy the surroundings. After trying the waters, visitors can take a stroll through the nearby Elisengarten park, a tranquil oasis in the heart of the city.",
                coordinates = new Coordinates
                {
                    latitude = 50.774130294261816, longitude = 6.0868675712478755, altitude = 230, metersOverGround = 10
                },
                subPointOfInterests = new List<SubPointOfInterest>
                {
                    new()
                    {
                        title = "Elisengarten Park",
                        description =
                            "Elisengarten is a tranquil park located near the Elisenbrunnen. The park is a popular spot for locals and tourists to relax and enjoy the surroundings.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77425140075906, longitude = 6.086219640165054, altitude = 230,
                            metersOverGround = 1
                        },
                        sourceTitle = "Elisengarten"
                    },
                    new()
                    {
                        title = "Archaeological Window",
                        description =
                            "The Archaeological Window provides a glimpse into Aachen's Roman past. Visitors can view the remains of a Roman bathhouse discovered during excavations in the area.",
                        coordinates = new Coordinates
                        {
                            latitude = 50.77443842954464, longitude = 6.086034889598118, altitude = 230,
                            metersOverGround = 1
                        },
                        sourceTitle = "Elisengarten"
                    }
                }
            }
        };

        [ContextMenu("Delete All Content")]
        public void DeleteAllContent()
        {
            DeleteAllTourContent();
            DeleteAllQandA();
        }

        [ContextMenu("Delete All Tour Content")]
        public void DeleteAllTourContent()
        {
            for (var i = 0; i < pointOfInterests.Length; i++) pointOfInterests[i].tourContent = null;
        }

        [ContextMenu("Delete All QandA")]
        public void DeleteAllQandA()
        {
            for (var i = 0; i < pointOfInterests.Length; i++) pointOfInterests[i].qandA = null;
        }

        [Serializable]
        public struct POI
        {
            public string title;
            public string subtitle;
            [HideInInspector] public string information;
            public List<InformationController.QAndA> qandA;
            public bool isPersonalized;
            public bool isCompletedVisit;
            public Coordinates coordinates;
            [NonSerialized] public GameObject gameObjectLocation;
            public TourContent[] tourContent;
            public List<SubPointOfInterest> subPointOfInterests;
        }

        [Serializable]
        public struct SubPointOfInterest
        {
            public string title;
            public string description;
            public string sourceTitle;
            public Coordinates coordinates;
            [NonSerialized] public GameObject gameObjectLocation;
        }

        [Serializable]
        public struct Coordinates
        {
            public double longitude;
            public double latitude;
            public double altitude;
            public double metersOverGround;
        }
    }
}