using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshDeformation {
        [StructLayout(LayoutKind.Sequential)]
        public struct VertexData {
               public Vector3 Position; // Odpowiada pozycji wierzchołka w przestrzeni 3D. Każda pozycja jest punktem w lokalnym układzie współrzędnych modelu.
                public Vector3 Normal; // Wektor normalny w wierzchołku, reprezentujący kierunek prostopadły do powierzchni. Normalne są kluczowe dla obliczeń oświetlenia.
                public Vector4 Tangent; // Wektor styczny, z czwartą składową (w) reprezentującą skrętność przestrzeni stycznej. Styczne są używane do mapowania normalnych.
                public Vector4 Color; // Kolor wierzchołka, w tym przezroczystość alfa. Kolory wierzchołków mogą być używane do barwienia, malowania wierzchołków lub innych efektów.
                public Vector4 Uv0; // Pierwszy kanał UV, który przechowuje współrzędne tekstury. Vector4 służy do przechowywania dodatkowych danych poza standardowym mapowaniem UV.
                public Vector4 Uv1; // Drugi kanał UV, podobny do Uv0, ale może być używany do dodatkowych celów teksturowania, takich jak mapy światła lub tekstury szczegółów.
              // Układ struktury, który jest sekwencyjny, zapewnia, że dane są rozmieszczone w pamięci dokładnie tak, jak zadeklarowano, co jest ważne dla interoperacyjności z natywnym kodem i ze względów wydajnościowych podczas przetwarzania danych w zadaniach.
        }
}

/* Co tu sie kurwa dzieje ?

Position: Vector3 jest używany, ponieważ pozycje są definiowane w przestrzeni 3D za pomocą współrzędnych x, y i z.
Normal: Również Vector3, ponieważ normalne reprezentują kierunek w przestrzeni 3D, wskazując, jak powierzchnia jest zorientowana względem oświetlenia.
Styczna: Vector4, gdzie pierwsze trzy składowe (x, y, z) reprezentują kierunek stycznej, a czwarta składowa (w) jest znakiem binormalnym, który jest niezbędny do prawidłowego mapowania wypukłości.
Color: Vector4 do przechowywania kanałów czerwonego, zielonego, niebieskiego i alfa, umożliwiając szczegółową reprezentację kolorów i efekty przezroczystości.
Uv0 i Uv1: Oba są Vector4, aby potencjalnie przechowywać bardziej złożone dane współrzędnych tekstury, takie jak dodatkowe wektory dla zaawansowanych technik cieniowania.
Atrybuty te są pobierane z tablicy danych siatki za pomocą funkcji GetVertexAttributeFormat, która zapewnia, że format określony w VertexData odpowiada rzeczywistemu formatowi siatki. Na przykład, jeśli siatka ma swoje UV przechowywane jako Vector4, jest to odzwierciedlone zarówno w strukturze VertexData, jak i VertexAttributeDescriptor.

Metoda CreateMeshData pobiera dane siatki i przygotowuje tablicę VertexAttributeDescriptor, która opisuje format i rozmiar każdego atrybutu wierzchołka. Ustawia również SubMeshDescriptor, wskazując, w jaki sposób wierzchołki są pogrupowane w submeshe, które mogą reprezentować różne części lub materiały siatki. */