import { useWindowDimensions } from 'react-native';

/** Returns true when width >= 768px (iPad portrait and above). */
export function useTablet(): boolean {
    const { width } = useWindowDimensions();
    return width >= 768;
}
