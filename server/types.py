from pydantic import BaseModel


class PointRequest(BaseModel):
    box_batch: list[tuple[int, int, int, int]]
    frames: bytes | str
