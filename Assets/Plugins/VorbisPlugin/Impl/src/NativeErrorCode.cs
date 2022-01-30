﻿namespace OggVorbis
{
    public enum NativeErrorCode
    {
        ERROR_INVALID_FILEPATH_PARAMETER = -1,
        ERROR_CANNOT_OPEN_FILE_FOR_WRITE = -2,
        ERROR_CANNOT_OPEN_FILE_FOR_READ = -3,
        ERROR_INPUT_FILESTREAM_IS_NOT_OGG_STREAM = -4,
        ERROR_READING_OGG_STREAM = -5,

        ERROR_INVALID_SAMPLES_PARAMETER = -10,
        ERROR_INVALID_SAMPLESLENGTH_PARAMETER = -11,
        ERROR_INVALID_CHANNELS_PARAMETER = -12,
        ERROR_INVALID_FREQUENCY_PARAMETER = -13,
        ERROR_INVALID_BASE_QUALITY_PARAMETER = -14,
        ERROR_MALLOC_RETURNED_NULL = -15,
        ERROR_BYTES_MEMORY_ARRAY_NULL = -16,
        ERROR_INVALID_WRITE_CALLBACK_PARAMETER = -17,
    }
}
