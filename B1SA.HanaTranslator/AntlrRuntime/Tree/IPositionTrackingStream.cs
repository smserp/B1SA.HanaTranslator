namespace Antlr.Runtime.Tree
{
    /**
     *
     * @author Sam Harwell
     */
    public interface IPositionTrackingStream
    {
        /**
         * Returns an element containing concrete information about the current
         * position in the stream.
         *
         * @param allowApproximateLocation if {@code false}, this method returns
         * {@code null} if an element containing exact information about the current
         * position is not available
         */
        object GetKnownPositionElement(bool allowApproximateLocation);

        /**
         * Determines if the specified {@code element} contains concrete position
         * information.
         *
         * @param element the element to check
         * @return {@code true} if {@code element} contains concrete position
         * information, otherwise {@code false}
         */
        bool HasPositionInformation(object element);

    }
}
